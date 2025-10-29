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

            var slots = await _db.GetTimeSlotsAsync();
            SlotPicker.ItemsSource = slots.Values.ToList();

            // Load addresses for current user (if available)
            if (!string.IsNullOrEmpty(_auth.CurrentUserId))
            {
                var addrs = await _db.GetUserAddressesAsync(_auth.CurrentUserId);
                AddressPicker.ItemsSource = addrs.Values.ToList();
            }

            StatusLabel.Text = $"Loaded {_foods.Count} menu items.";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Error loading foods.";
            System.Diagnostics.Debug.WriteLine($"[ERROR] LoadDataAsync: {ex.Message}");
        }
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
        if (string.IsNullOrEmpty(_auth.CurrentIdToken) || string.IsNullOrEmpty(_auth.CurrentUserId))
        {
            await DisplayAlert("Not Signed In", "Please sign in again.", "OK");
            return;
        }

        // 10:00 AM cutoff
        if (DateTime.Now.TimeOfDay >= new TimeSpan(10, 0, 0))
        {
            await DisplayAlert("Too Late", "Orders must be placed before 10:00 AM.", "OK");
            return;
        }

        var city = CityPicker.SelectedItem as City;
        var slot = SlotPicker.SelectedItem as TimeSlot;
        var addr = AddressPicker.SelectedItem as DeliveryAddress;

        if (city == null || slot == null || addr == null)
        {
            await DisplayAlert("Missing Info", "Please select city, time slot, and address.", "OK");
            return;
        }

        bool any = false;

        foreach (var food in _foods)
        {
            var q = _qty.TryGetValue(food.Food_ID, out var val) ? val : 0;
            if (q <= 0) continue;

            any = true;
            var selected = _selectedOption.TryGetValue(food.Food_ID, out var opt) ? opt : "";

            var order = new Order
            {
                Order_ID = Guid.NewGuid().ToString(),
                Order_Date = DateTime.UtcNow,
                Quantity = q,
                Food_ID = food.Food_ID,
                City_ID = city.City_ID,
                TimeSlot_ID = slot.TimeSlot_ID,
                User_ID = _auth.CurrentUserId!,
                Address_ID = addr.Address_ID,
                Option_Key = food.Option_Key,
                Option_Value = selected,
                Total_Price = food.Price * q
            };

            var ok = await _db.PlaceOrderAsync(_auth.CurrentUserId!, order);
            if (!ok)
            {
                await DisplayAlert("Error", $"Failed to place {food.Food_Name}.", "OK");
                return;
            }
        }

        if (!any)
        {
            await DisplayAlert("No Items", "Add at least one meal.", "OK");
            return;
        }

        await DisplayAlert("Success", "Your order has been placed!", "OK");
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
