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
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        StatusMessage = "Registering...";
        var uid = await _auth.SignUpAsync(Email, Password);

        if (uid == null)
        {
            StatusMessage = "Failed to register.";
            return;
        }

        var user = new User
        {
            User_ID = uid,
            User_Email = Email,
            Password = Password, // ⚠️ For assignment only; normally don’t store plaintext
            First_Name = FirstName,
            Last_Name = LastName,
            Mobile = Mobile
        };

        await _db.Conn.InsertAsync(user);

        StatusMessage = "Registration successful!";
        await Shell.Current.GoToAsync("//Login");
    }

    [RelayCommand]
    private async Task GoToLoginAsync()
    {
        await Shell.Current.GoToAsync("//Login");
    }
}
