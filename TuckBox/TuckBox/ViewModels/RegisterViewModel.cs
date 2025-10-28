using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;
using TuckBox.Models;
using TuckBox.Data;
using System.Text.Json;

namespace TuckBox.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _cloudDb;   // 🔹 added
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
        System.Diagnostics.Debug.WriteLine("[DEBUG] RegisterViewModel initialized with Firebase + Local DB.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Register attempt email={Email}, name={FirstName} {LastName}");

        // -----------------------
        // ✅ Input validation
        // -----------------------
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(FirstName)) errors.Add("First name is required.");
        if (string.IsNullOrWhiteSpace(LastName)) errors.Add("Last name is required.");
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@")) errors.Add("Valid email is required.");
        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6) errors.Add("Password must be at least 6 characters.");
        if (string.IsNullOrWhiteSpace(Mobile)) errors.Add("Mobile number is required.");

        if (errors.Count > 0)
        {
            StatusMessage = string.Join("\n", errors);
            return;
        }

        // -----------------------
        // ✅ Firebase Auth sign-up
        // -----------------------
        StatusMessage = "Creating account...";
        var uid = await _auth.SignUpAsync(Email, Password);

        if (uid == null)
        {
            StatusMessage = "Failed to register. Please try again.";
            System.Diagnostics.Debug.WriteLine("[DEBUG] Register failed — UID null from Firebase.");
            return;
        }

        // -----------------------
        // ✅ Build profile model
        // -----------------------
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var profile = new User
        {
            User_ID = uid,
            User_Email = Email.Trim(),
            Password = Password, // ⚠️ only for this assignment
            First_Name = FirstName.Trim(),
            Last_Name = LastName.Trim(),
            Mobile = Mobile.Trim(),
            Created_Utc = now,
            Updated_Utc = now
        };

        // -----------------------
        // ✅ Upload to Firebase RTDB
        // -----------------------
        try
        {
            var json = JsonSerializer.Serialize(profile);
            var url = $"{_cloudDb.DbUrl}/Users/{uid}.json"; // no auth param for open rules
            var resp = await _cloudDb.Http.PutAsync(url,
                new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

            System.Diagnostics.Debug.WriteLine($"[DEBUG] RTDB upsert status={resp.StatusCode}");
            if (!resp.IsSuccessStatusCode)
            {
                StatusMessage = "Cloud save failed.";
                return;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Error connecting to Firebase.";
            System.Diagnostics.Debug.WriteLine($"[ERROR] Cloud save exception: {ex}");
            return;
        }

        // -----------------------
        // ✅ Save locally
        // -----------------------
        await _localDb.Conn.InsertOrReplaceAsync(profile);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Profile stored locally for UID={uid}");

        // -----------------------
        // ✅ Success feedback
        // -----------------------
        StatusMessage = "Registration successful!";
        await Shell.Current.GoToAsync("//Login");
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] Navigating to Login page.");
        await Shell.Current.GoToAsync("//Login");
    }
}
