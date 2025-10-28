using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;
using TuckBox.Models;
using TuckBox.Data;

namespace TuckBox.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _cloudDb;
    private readonly AppDb _localDb;

    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string mobile = "";
    [ObservableProperty] private string statusMessage = "";

    public RegisterViewModel(FirebaseAuthService auth, FirebaseDbService cloudDb, AppDb localDb)
    {
        _auth = auth;
        _cloudDb = cloudDb;
        _localDb = localDb;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        // Validate
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(LastName)) errors.Add("Last name is required.");
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@")) errors.Add("Valid email is required.");
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6) errors.Add("Password must be at least 6 characters.");
        if (string.IsNullOrWhiteSpace(Mobile)) errors.Add("Mobile number is required.");
        if (errors.Count > 0) { StatusMessage = string.Join("\n", errors); return; }

        StatusMessage = "Creating account...";
        var uid = await _auth.SignUpAsync(Email, Password);
        if (uid is null) { StatusMessage = "Registration failed."; return; }

        // Build profile
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var profile = new User
        {
            User_ID = uid,
            User_Email = Email.Trim(),
            Password = Password, // ⚠️ assignment only
            First_Name = FirstName.Trim(),
            Last_Name = LastName.Trim(),
            Mobile = Mobile.Trim(),
            Created_Utc = now,
            Updated_Utc = now
        };

        // Ensure we have a token
        if (string.IsNullOrEmpty(_auth.CurrentIdToken))
        {
            // small wait to allow token propagation
            await Task.Delay(300);
            if (string.IsNullOrEmpty(_auth.CurrentIdToken))
            {
                StatusMessage = "Could not get auth token.";
                return;
            }
        }

        // Cloud write
        StatusMessage = "Saving profile...";
        var ok = await _cloudDb.UpsertUserProfileAsync(profile, _auth.CurrentIdToken!);
        if (!ok) { StatusMessage = "Failed to save profile to cloud."; return; }

        // Local mirror
        await _localDb.Conn.InsertOrReplaceAsync(profile);

        StatusMessage = "Registration successful!";
        await Shell.Current.GoToAsync("Login"); // relative (route-only page)
    }

    [RelayCommand]
    private async Task GoToLoginAsync() => await Shell.Current.GoToAsync("Login");
}
