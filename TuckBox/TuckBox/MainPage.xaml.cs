namespace TuckBox;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Models;
using TuckBox.Services;
using System.Windows.Input;

public partial class MainPage : ContentPage
{
    private readonly FirebaseDbService _db;
    private readonly FirebaseAuthService _auth;
    private readonly ObservableCollection<City> _cities = new();

    // Toolbar + button commands
    public ICommand GoHomeCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync("Main"));
    public ICommand GoPlaceOrderCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync("PlaceOrder"));
    public ICommand GoUpdateUserCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync("UpdateUser"));
    public ICommand GoCurrentOrderCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync("CurrentOrder"));
    public ICommand GoOrderHistoryCommand => new AsyncRelayCommand(() => Shell.Current.GoToAsync("OrderHistory"));
    public ICommand SignOutCommand => new AsyncRelayCommand(SignOutAsync);

    public MainPage(FirebaseDbService dbService, FirebaseAuthService auth)
    {
        InitializeComponent();
        _db = dbService;
        _auth = auth;

        BindingContext = this;        // <-- needed for ToolbarItem/Button bindings
        MoreItem.Clicked += OnMoreClicked;

    }

    private async void OnMoreClicked(object? sender, EventArgs e)
    {
        var choice = await DisplayActionSheet("Menu", "Cancel", null,
            "Home", "Place Order", "Update User", "Current Order", "Order History", "Sign Out");

        switch (choice)
        {
            case "Home":
                await Shell.Current.GoToAsync("Main");
                break;
            case "Place Order":
                await Shell.Current.GoToAsync("PlaceOrder");
                break;
            case "Update User":
                await Shell.Current.GoToAsync("UpdateUser");
                break;
            case "Current Order":
                await Shell.Current.GoToAsync("CurrentOrder");
                break;
            case "Order History":
                await Shell.Current.GoToAsync("OrderHistory");
                break;
            case "Sign Out":
                await SignOutAsync();
                break;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
    }

    private async Task SignOutAsync()
    {
        try
        {
            _auth.SignOut(); // clear tokens/secure storage
            await Shell.Current.GoToAsync("Login");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignOut ERROR] {ex}");
        }
    }
}
