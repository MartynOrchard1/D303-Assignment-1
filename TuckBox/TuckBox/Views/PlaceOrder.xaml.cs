using System.Collections.ObjectModel;
using TuckBox.Models;
using TuckBox.Services;

namespace TuckBox.Views;

public partial class PlaceOrder : ContentPage
{
    private readonly FirebaseDbService _db;
    private readonly FirebaseAuthService _auth;

    private readonly ObservableCollection<City> _cities = new();
    private readonly ObservableCollection<TimeSlot> _slots = new();
    private readonly ObservableCollection<DeliveryAddress> _addresses = new();
    private readonly ObservableCollection<Food> _foods = new();

    // Track selections
    private readonly Dictionary<string, int> _qty = new();                 // Food_ID -> qty
    private readonly Dictionary<string, string> _selectedOption = new();   // Food_ID -> Option_Value

    public PlaceOrder(FirebaseDbService db, FirebaseAuthService auth)
    {
        InitializeComponent();
        _db = db;
        _auth = auth;

        CityPicker.ItemsSource = _cities;
        SlotPicker.ItemsSource = _slots;
        AddressPicker.ItemsSource = _addresses;
        FoodsList.ItemsSource = _foods;

        FoodsList.Loaded += (_, __) => PopulateFoodOptions();
        FoodsList.Scrolled += (_, __) => PopulateFoodOptions(); // ensure recycled cells get options
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadReferenceDataAsync();
    }

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            StatusLabel.Text = "Loading...";
            _cities.Clear();
            foreach (var c in (await _db.GetCitiesAsync()).Values) _cities.Add(c);

            _slots.Clear();
            foreach (var s in (await _db.GetTimeSlotsAsync()).Values) _slots.Add(s);

            _addresses.Clear();
            if (!string.IsNullOrEmpty(_auth.CurrentIdToken) && !string.IsNullOrEmpty(_auth.CurrentUid))
            {
                var dict = await _db.GetUserAddressesAsync(_auth.CurrentUid!, _auth.CurrentIdToken!);
                foreach (var a in dict.Values) _addresses.Add(a);
            }

            _foods.Clear();
            foreach (var f in (await _db.GetFoodsAsync()).Values)
            {
                _foods.Add(f);
                _qty[f.Food_ID] = 0;
                _selectedOption[f.Food_ID] = f.Option_Values?.FirstOrDefault() ?? "";
            }

            StatusLabel.Text = "";
            PopulateFoodOptions();
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Failed to load data.";
            System.Diagnostics.Debug.WriteLine($"[PlaceOrder] Load data error: {ex}");
        }
    }

    // Populate radio buttons for each food’s options inside the template
    private void PopulateFoodOptions()
    {
        foreach (var cell in FoodsList.VisibleCells)
        {
            if (cell is not ContentView cv) continue;

            var host = cv.FindByName<HorizontalStackLayout>("OptionsHost");
            var food = cv.BindingContext as Food;
            if (host == null || food == null) continue;

            host.Children.Clear();

            var values = food.Option_Values ?? Array.Empty<string>();
            var group = $"grp_{food.Food_ID}"; // unique group per food row

            // Choose current or default option
            var current = _selectedOption.TryGetValue(food.Food_ID, out var sel)
                ? sel
                : values.FirstOrDefault() ?? "";

            foreach (var val in values)
            {
                var rb = new RadioButton
                {
                    Content = val,
                    Value = val,
                    GroupName = group,
                    IsChecked = (val == current),
                    Margin = new Thickness(0, 0, 8, 0)
                };

                rb.CheckedChanged += (_, e) =>
                {
                    if (e.Value is string v && e.IsChecked)
                        _selectedOption[food.Food_ID] = v;
                };

                host.Children.Add(rb);
            }

            // Ensure dictionary has at least a default stored
            _selectedOption[food.Food_ID] = current;
        }
    }


    private void OnPlusClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        var cv = btn.FindParent<ContentView>();
        var food = cv?.BindingContext as Food;
        var qtyLabel = cv?.FindByName<Label>("QtyLabel");
        if (food == null || qtyLabel == null) return;

        _qty[food.Food_ID] = (_qty.TryGetValue(food.Food_ID, out var q) ? q : 0) + 1;
        qtyLabel.Text = _qty[food.Food_ID].ToString();
    }

    private void OnMinusClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        var cv = btn.FindParent<ContentView>();
        var food = cv?.BindingContext as Food;
        var qtyLabel = cv?.FindByName<Label>("QtyLabel");
        if (food == null || qtyLabel == null) return;

        var q = _qty.TryGetValue(food.Food_ID, out var current) ? current : 0;
        q = Math.Max(0, q - 1);
        _qty[food.Food_ID] = q;
        qtyLabel.Text = q.ToString();
    }

    private async void OnAddAddressClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_auth.CurrentIdToken) || string.IsNullOrEmpty(_auth.CurrentUid))
        {
            await DisplayAlert("Not Signed In", "Please sign in again.", "OK");
            return;
        }

        var text = await DisplayPromptAsync("Add Address", "Enter delivery address:");
        if (string.IsNullOrWhiteSpace(text)) return;

        var addr = new DeliveryAddress
        {
            Address_ID = Guid.NewGuid().ToString("N"),
            Address = text.Trim(),
            UsersUser_ID = _auth.CurrentUid!
        };

        var ok = await _db.UpsertUserAddressAsync(_auth.CurrentUid!, addr, _auth.CurrentIdToken!);
        if (ok)
        {
            _addresses.Add(addr);
            AddressPicker.SelectedItem = addr;
        }
        else
        {
            await DisplayAlert("Error", "Could not save address.", "OK");
        }
    }

    private bool IsBeforeCutoff()
    {
        // Assignment requires orders before 10:00 — use device local time
        var now = DateTime.Now.TimeOfDay;
        return now < new TimeSpan(10, 0, 0);
    }

    private async void OnPlaceOrderClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_auth.CurrentIdToken) || string.IsNullOrEmpty(_auth.CurrentUid))
        {
            await DisplayAlert("Not Signed In", "Please sign in again.", "OK");
            return;
        }

        if (!IsBeforeCutoff())
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

        // Build items from selections
        var items = new List<OrderItem>();
        foreach (var food in _foods)
        {
            var q = _qty.TryGetValue(food.Food_ID, out var val) ? val : 0;
            if (q <= 0) continue;

            var sel = _selectedOption.TryGetValue(food.Food_ID, out var opt) ? opt : "";
            items.Add(new OrderItem
            {
                Food_ID = food.Food_ID,
                Food_Name = food.Food_Name,
                Option_Key = food.Option_Key,
                Option_Value = sel,
                Quantity = q,
                UnitPrice = food.Price
            });
        }

        if (items.Count == 0)
        {
            await DisplayAlert("No Items", "Add at least one meal.", "OK");
            return;
        }

        var total = items.Sum(i => i.LineTotal);
        var order = new Order
        {
            Order_ID = Guid.NewGuid().ToString("N"),
            Order_DateUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            CityCity_ID = city.City_ID,
            TimeSlotsTime_Slot_ID = slot.TimeSlot_ID,
            Address_ID = addr.Address_ID,
            UserUser_ID = _auth.CurrentUid!,
            Items = items,
            Total = total
        };

        StatusLabel.Text = "Placing order...";
        var ok = await _db.PlaceOrderAsync(_auth.CurrentUid!, order, _auth.CurrentIdToken!);
        if (ok)
        {
            StatusLabel.Text = "Order placed!";
            await DisplayAlert("Success", $"Your order total is ${total:F2}.", "OK");
            await Shell.Current.GoToAsync("CurrentOrder");
        }
        else
        {
            StatusLabel.Text = "Failed to place order.";
            await DisplayAlert("Error", "Could not place order. Please try again.", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("Main");
    }
}

// helper to find parent cell elements
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
