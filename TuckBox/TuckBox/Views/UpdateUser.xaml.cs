using TuckBox.Services;
using TuckBox.Models;
using TuckBox.Data;

namespace TuckBox.Views;

public partial class UpdateUser : ContentPage
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _db;
    private readonly AppDb _localDb;   // for local mirror
    private User? _currentUser;

    public UpdateUser(FirebaseAuthService auth, FirebaseDbService db, AppDb localDb)
    {
        InitializeComponent();
        _auth = auth;
        _db = db;
        _localDb = localDb;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrEmpty(_auth.CurrentUserId) || string.IsNullOrEmpty(_auth.CurrentIdToken))
        {
            await DisplayAlert("Not signed in", "Please sign in again.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        // load cities for picker
        await LoadCities();

        // load user profile
        await LoadUser();
    }

    private async Task LoadCities()
    {
        try
        {
            var cities = await _db.GetCitiesAsync();
            CityPicker.ItemsSource = cities.Values.ToList();
            CityPicker.ItemDisplayBinding = new Binding("City_Name");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateUser] load cities failed: {ex}");
        }
    }

    private async Task LoadUser()
    {
        try
        {
            var user = await _db.GetUserProfileAsync(_auth.CurrentUserId!, _auth.CurrentIdToken!);
            _currentUser = user;

            if (user != null)
            {
                FirstNameEntry.Text = user.First_Name;
                LastNameEntry.Text = user.Last_Name;
                EmailEntry.Text = user.User_Email;
                MobileEntry.Text = user.Mobile;

                // address if you have it
                AddressEntry.Text = user.Delivery_Address;

                // try match city in picker
                if (!string.IsNullOrEmpty(user.City_ID) && CityPicker.ItemsSource is List<City> cities)
                {
                    var match = cities.FirstOrDefault(c => c.City_ID == user.City_ID);
                    if (match != null)
                        CityPicker.SelectedItem = match;
                }
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Failed to load profile.";
            System.Diagnostics.Debug.WriteLine($"[UpdateUser] load user failed: {ex}");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_currentUser == null)
        {
            StatusLabel.Text = "User data not loaded.";
            return;
        }

        // basic validation
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text))
        {
            StatusLabel.Text = "First name is required.";
            return;
        }

        // update object
        _currentUser.First_Name = FirstNameEntry.Text?.Trim() ?? "";
        _currentUser.Last_Name = LastNameEntry.Text?.Trim() ?? "";
        _currentUser.Mobile = MobileEntry.Text?.Trim() ?? "";
        _currentUser.Delivery_Address = AddressEntry.Text?.Trim() ?? "";

        if (CityPicker.SelectedItem is City selectedCity)
        {
            _currentUser.City_ID = selectedCity.City_ID;
            _currentUser.City_Name = selectedCity.City_Name;
        }

        try
        {
            // 1) push to Firebase
            var ok = await _db.UpsertUserProfileAsync(_currentUser, _auth.CurrentIdToken!);
            if (!ok)
            {
                StatusLabel.Text = "Failed to update in cloud.";
                return;
            }

            // 2) update local SQLite (best effort)
            await _localDb.Conn.InsertOrReplaceAsync(_currentUser);

            StatusLabel.TextColor = Colors.Green;
            StatusLabel.Text = "Profile updated.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Error saving profile.";
            System.Diagnostics.Debug.WriteLine($"[UpdateUser] save failed: {ex}");
        }
    }

    // toolbar commands
    public Command GoHomeCommand => new(async () => await Shell.Current.GoToAsync("Main"));
    public Command GoPlaceOrderCommand => new(async () => await Shell.Current.GoToAsync("PlaceOrder"));
    public Command GoOrderHistoryCommand => new(async () => await Shell.Current.GoToAsync("OrderHistory"));
    public Command SignOutCommand => new(async () =>
    {
        _auth.SignOut();
        await Shell.Current.GoToAsync("//Login");
    });
}