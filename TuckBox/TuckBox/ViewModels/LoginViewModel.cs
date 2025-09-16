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
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        StatusMessage = "Signing in...";
        var uid = await _auth.SignInAsync(Email, Password);

        if (uid != null)
        {
            StatusMessage = "Login successful!";
            await Shell.Current.GoToAsync("//Main");
        }
        else
        {
            StatusMessage = "Invalid credentials.";
        }
    }

    [RelayCommand]
    private async Task GoToRegisterAsync()
    {
        await Shell.Current.GoToAsync("//Register");
    }
}
