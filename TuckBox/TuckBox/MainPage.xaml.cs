namespace TuckBox;
using System.Collections.ObjectModel;
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

        BindingContext = this;        // toolbar/button bindings

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
        await LoadCurrentOrder();   // 👈 NEW
        await LoadUserName();   // 👈 NEW

    }

    private async Task LoadUserName()
    {
        try
        {
            if (string.IsNullOrEmpty(_auth.CurrentUserId) || string.IsNullOrEmpty(_auth.CurrentIdToken))
            {
                WelcomeLabel.Text = "WELCOME";
                return;
            }

            var profile = await _db.GetUserProfileAsync(_auth.CurrentUserId, _auth.CurrentIdToken);

            if (profile != null && !string.IsNullOrEmpty(profile.First_Name))
                WelcomeLabel.Text = $"WELCOME, {profile.First_Name.ToUpper()}!";
            else
                WelcomeLabel.Text = "WELCOME";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadUserName failed: {ex}");
            WelcomeLabel.Text = "WELCOME";
        }
    }

    // 👇 NEW: pull latest order for current user and show in label
    private async Task LoadCurrentOrder()
    {
        try
        {
            if (string.IsNullOrEmpty(_auth.CurrentUserId))
            {
                CurrentOrderLabel.Text = "No current order.";
                return;
            }

            var orders = await _db.GetOrdersForUserAsync(_auth.CurrentUserId);
            if (orders.Count == 0)
            {
                CurrentOrderLabel.Text = "No current order.";
                return;
            }

            // pick latest by date string (we saved dd/MM/yyyy HH:mm:ss)
            var latest = orders
                .Select(kv => kv.Value)
                .OrderByDescending(o => o.Order_Date)
                .First();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Order: {latest.Order_ID}");
            sb.AppendLine($"Date: {latest.Order_Date}");
            if (!string.IsNullOrEmpty(latest.City_Name))
                sb.AppendLine($"City: {latest.City_Name}");
            if (!string.IsNullOrEmpty(latest.Time_Slot))
                sb.AppendLine($"Time: {latest.Time_Slot}");
            sb.AppendLine($"Total: {latest.Total_Price:C}");

            // show first item if you want
            if (latest.Items != null && latest.Items.Count > 0)
            {
                var first = latest.Items.Values.First();
                sb.AppendLine($"1st item: {first.Food_Name} x{first.Quantity}");
            }

            CurrentOrderLabel.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            CurrentOrderLabel.Text = "Could not load current order.";
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadCurrentOrder failed: {ex}");
        }
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
