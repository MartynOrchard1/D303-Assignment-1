using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;
using TuckBox.Models;
using TuckBox.Data;

namespace TuckBox.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    private readonly FirebaseAuthService _auth;
    private readonly AppDb _db;

    [ObservableProperty] private string firstName = "";
    [ObservableProperty] private string lastName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string mobile = "";
    [ObservableProperty] private string statusMessage = "";

    public RegisterViewModel(FirebaseAuthService auth, AppDb db)
    {
        _auth = auth;
        _db = db;
        System.Diagnostics.Debug.WriteLine("[DEBUG] RegisterViewModel initialized.");
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Register attempt email={Email}, name={FirstName} {LastName}");

        StatusMessage = "Registering...";
        var uid = await _auth.SignUpAsync(Email, Password);

        if (uid == null)
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Register failed (Firebase returned null).");
            StatusMessage = "Failed to register.";
            return;
        }

        var user = new User
        {
            User_ID = uid,
            User_Email = Email,
            Password = Password, // ⚠️ For assignment only
            First_Name = FirstName,
            Last_Name = LastName,
            Mobile = Mobile
        };

        await _db.Conn.InsertAsync(user);
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Registered user saved locally with UID={uid}");

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
