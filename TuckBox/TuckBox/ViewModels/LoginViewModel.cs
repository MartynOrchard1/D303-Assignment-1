using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;

namespace TuckBox.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly FirebaseAuthService _auth;
    private readonly string _googleClientId;
    private readonly string _googleRedirectUri;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string statusMessage = "";

    public LoginViewModel(FirebaseAuthService auth, string googleClientId, string googleRedirectUri)
    {
        _auth = auth;
        _googleClientId = googleClientId;
        _googleRedirectUri = googleRedirectUri;
        System.Diagnostics.Debug.WriteLine("[DEBUG] LoginViewModel initialized for Google sign-in.");
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

            // ✅ confirm token is set
            if (string.IsNullOrEmpty(_auth.CurrentIdToken))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Waiting for ID token...");
                await Task.Delay(500); // short delay ensures async completion
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] ID Token available: {!string.IsNullOrEmpty(_auth.CurrentIdToken)}");

            await Shell.Current.GoToAsync("Main");
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
        try
        {
            await Shell.Current.GoToAsync("Register");

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NAV ERROR] {ex.GetType().Name}: {ex.Message}\n{ex}");
        }
    }

    [RelayCommand]
    private async Task LoginWithGoogleAsync()
    {
        var httpsRedirect = _googleRedirectUri; // from appsettings, e.g. https://MartynOrchard1.github.io/
        var customCallback = "com.googleusercontent.apps.971309845644-atoo2nl2ceqjvbdhmo4iqdt7othvkr09:/oauth2redirect";

        var uid = await _auth.SignInWithGoogleAsync(_googleClientId, httpsRedirect, customCallback);

        if (uid != null)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google login success. Firebase UID={uid}");
            await Shell.Current.GoToAsync("Main");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] Google login failed.");
            StatusMessage = "Google sign-in failed. Try again.";
        }
    }
}
