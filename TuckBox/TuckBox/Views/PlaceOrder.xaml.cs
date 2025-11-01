using System.Collections.ObjectModel;
using TuckBox.Models;
using TuckBox.Services;

namespace TuckBox.Views;

public partial class PlaceOrder : ContentPage
{
    private readonly FirebaseDbService _db;
    private readonly FirebaseAuthService _auth;

    private readonly ObservableCollection<Food> _foods = new();
    private readonly Dictionary<string, int> _qty = new();                 // Food_ID -> qty
    private readonly Dictionary<string, string> _selectedOption = new();   // Food_ID -> option value

    public PlaceOrder(FirebaseDbService db, FirebaseAuthService auth)
    {
        InitializeComponent();
        _db = db;
        _auth = auth;
        FoodsList.ItemsSource = _foods;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            StatusLabel.Text = "Loading menu...";
            _foods.Clear();

            // Load foods
            var foods = await _db.GetFoodsAsync();
            foreach (var item in foods.Values)
                _foods.Add(item);

            // Load cities & slots
            var cities = await _db.GetCitiesAsync();
            CityPicker.ItemsSource = cities.Values.ToList();
            // NEW: show city name
            CityPicker.ItemDisplayBinding = new Binding(nameof(City.City_Name));


            var slots = await _db.GetTimeSlotsAsync();
            SlotPicker.ItemsSource = slots.Values.ToList();
           
            // NEW: show time slot text
            SlotPicker.ItemDisplayBinding = new Binding(nameof(TimeSlot.Time_Slot));

            // Load addresses for current user
            if (!string.IsNullOrEmpty(_auth.CurrentUserId))
            {
                var addrs = await _db.GetUserAddressesAsync(_auth.CurrentUserId);
                AddressPicker.ItemsSource = addrs.Values.ToList();
                // NEW: show address line
                AddressPicker.ItemDisplayBinding = new Binding(nameof(DeliveryAddress.Address));
            }

            StatusLabel.Text = $"Loaded {_foods.Count} menu items.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Error loading foods.";
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadDataAsync: {ex.Message}");
        }
    }

    private async void OnAddAddressClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_auth.CurrentUserId))
        {
            await DisplayAlert("Not Signed In", "Please sign in first.", "OK");
            return;
        }

        // ask for a single-line address (simple for the assignment)
        var text = await DisplayPromptAsync(
            "New Address",
            "Enter your delivery address:",
            maxLength: 200,
            keyboard: Keyboard.Text);

        if (string.IsNullOrWhiteSpace(text))
            return;

        var addr = new DeliveryAddress
        {
            Address_ID = Guid.NewGuid().ToString(),
            Address = text.Trim(),
            UsersUser_ID = _auth.CurrentUserId!
        };

        var ok = await _db.UpsertUserAddressAsync(_auth.CurrentUserId!, addr);
        if (!ok)
        {
            await DisplayAlert("Error", "Could not save address. Try again.", "OK");
            return;
        }

        // refresh list and preselect the new one
        var addrs = await _db.GetUserAddressesAsync(_auth.CurrentUserId!);
        var list = addrs.Values.ToList();
        AddressPicker.ItemsSource = list;
        AddressPicker.ItemDisplayBinding = new Binding(nameof(DeliveryAddress.Address));
        AddressPicker.SelectedItem = list.FirstOrDefault(a => a.Address_ID == addr.Address_ID);
    }


    // Radio selection handler for options
    private void OnOptionChecked(object sender, CheckedChangedEventArgs e)
    {
        // e.Value is bool (checked?)
        if (!e.Value) return;

        if (sender is not RadioButton rb) return;

        // Find the row root and get its binding context (Food)
        var rowRoot = rb.FindParent<Layout>();
        var food = rowRoot?.BindingContext as Food;
        if (food == null) return;

        // rb.Value is object; we expect string (the option value)
        var selected = rb.Value as string ?? string.Empty;
        _selectedOption[food.Food_ID] = selected;
    }

    // Quantity controls
    private void OnPlusClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        var cv = btn.FindParent<ContentView>();
        var food = cv?.BindingContext as Food;
        var qtyLabel = cv?.FindByName<Label>("QtyLabel");
        if (food == null || qtyLabel == null) return;

        var q = _qty.TryGetValue(food.Food_ID, out var cur) ? cur : 0;
        q++;
        _qty[food.Food_ID] = q;
        qtyLabel.Text = q.ToString(); // ✅ string
    }

    private void OnMinusClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        var cv = btn.FindParent<ContentView>();
        var food = cv?.BindingContext as Food;
        var qtyLabel = cv?.FindByName<Label>("QtyLabel");
        if (food == null || qtyLabel == null) return;

        var q = _qty.TryGetValue(food.Food_ID, out var cur) ? cur : 0;
        q = Math.Max(0, q - 1);
        _qty[food.Food_ID] = q;
        qtyLabel.Text = q.ToString(); // ✅ string
    }

    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        // 1. must be logged in
        if (string.IsNullOrEmpty(_auth.CurrentIdToken) || string.IsNullOrEmpty(_auth.CurrentUserId))
        {
            await DisplayAlert("Not Signed In", "Please sign in again.", "OK");
            return;
        }

        // 2. NZ cutoff check (10:00am NZ)
        var nzZone = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
        var nowNz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nzZone);
        if (nowNz.TimeOfDay >= new TimeSpan(10, 0, 0)) // Change the '10' to change the cutoff time (24H Clock)
        {
            await DisplayAlert("Too Late",
                $"Orders must be placed before 10:00 AM NZ time.\nCurrent NZ time: {nowNz:HH:mm}",
                "OK");
            return;
        }

        // 3. basic selections
        var city = CityPicker.SelectedItem as City;
        var slot = SlotPicker.SelectedItem as TimeSlot;
        var addr = AddressPicker.SelectedItem as DeliveryAddress;

        if (city == null || slot == null || addr == null)
        {
            await DisplayAlert("Missing Info", "Please select city, time slot, and delivery address.", "OK");
            return;
        }

        // 4. build item list from what user picked on screen
        var orderItems = new List<(Food food, int qty, string? option)>();
        bool any = false;

        foreach (var food in _foods)
        {
            var qty = _qty.TryGetValue(food.Food_ID, out var q) ? q : 0;
            if (qty <= 0) continue;

            any = true;
            _selectedOption.TryGetValue(food.Food_ID, out var optVal);

            orderItems.Add((food, qty, optVal));
        }

        if (!any)
        {
            await DisplayAlert("No Items", "Please select at least one meal.", "OK");
            return;
        }

        // 5. call the NEW service method ONCE
        var ok = await _db.PlaceOrderAsync(
            _auth.CurrentUserId!,
            city,
            slot,
            addr,
            orderItems);

        if (!ok)
        {
            await DisplayAlert("Error", "Could not place the order. Try again.", "OK");
            return;
        }

        await DisplayAlert("Success", "Your order has been placed.", "OK");
        await Shell.Current.GoToAsync("CurrentOrder");
    }
}

    // Small visual tree helper
    static class VisualTreeExtensions
{
    public static T? FindParent<T>(this Element element) where T : Element
    {
        Element? parent = element.Parent;
        while (parent != null && parent is not T)
            parent = parent.Parent;
        return parent as T;
    }
}
