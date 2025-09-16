using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;

namespace TuckBox.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly FirebaseAuthService _auth;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string statusMessage = "";

    public LoginViewModel(FirebaseAuthService auth)
    {
        _auth = auth;
        System.Diagnostics.Debug.WriteLine("[DEBUG] LoginViewModel initialized.");
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Login attempt with email={Email}");

        StatusMessage = "Signing in...";
        var uid = await _auth.SignInAsync(Email, Password);

        if (uid != null)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Login success, Firebase UID={uid}");
            StatusMessage = "Login successful!";
            await Shell.Current.GoToAsync("//Main");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Login failed (invalid credentials).");
            StatusMessage = "Invalid credentials.";
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        System.Diagnostics.Debug.WriteLine("[DEBUG] Navigating to Register page.");
        await Shell.Current.GoToAsync("//Register");
    }
}
