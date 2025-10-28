namespace TuckBox;
using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Models;
using TuckBox.Services;

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
        CitiesList.ItemsSource = _cities;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCities();
    }

    private async Task LoadCities()
    {
        try
        {
            StatusLabel.Text = "Loading cities...";
            var cities = await _db.GetCitiesAsync();

            _cities.Clear();
            foreach (var city in cities.Values)
                _cities.Add(city);

            StatusLabel.Text = _cities.Count > 0
                ? $"Loaded {_cities.Count} cities."
                : "No cities found.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Error loading cities.";
            System.Diagnostics.Debug.WriteLine($"[ERROR] Firebase fetch failed: {ex.Message}");
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
