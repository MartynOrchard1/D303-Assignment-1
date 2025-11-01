using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using TuckBox.Services;
using TuckBox.Models;

namespace TuckBox.Views;

public partial class OrderHistory : ContentPage
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _db;

    // what the XAML binds to
    public ObservableCollection<OrderDisplay> Orders { get; } = new();

    // toolbar commands
    public IAsyncRelayCommand RefreshCommand => new AsyncRelayCommand(LoadOrdersAsync);
    public Command GoHomeCommand => new(async () => await Shell.Current.GoToAsync("Main"));
    public Command GoPlaceOrderCommand => new(async () => await Shell.Current.GoToAsync("PlaceOrder"));
    public Command GoUpdateUserCommand => new(async () => await Shell.Current.GoToAsync("UpdateUser"));
    public Command SignOutCommand => new(async () =>
    {
        _auth.SignOut();
        await Shell.Current.GoToAsync("//Login");
    });

    public OrderHistory(FirebaseAuthService auth, FirebaseDbService db)
    {
        InitializeComponent();
        _auth = auth;
        _db = db;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        if (string.IsNullOrEmpty(_auth.CurrentUserId) || string.IsNullOrEmpty(_auth.CurrentIdToken))
        {
            await DisplayAlert("Not signed in", "Please sign in again.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        try
        {
            RefreshContainer.IsRefreshing = true;

            // this is the method we added earlier in FirebaseDbService
            var rawOrders = await _db.GetOrdersForUserAsync(_auth.CurrentUserId);

            Orders.Clear();

            // newest first
            foreach (var order in rawOrders.Values.OrderByDescending(o => o.Order_Date))
            {
                Orders.Add(ToDisplay(order));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OrderHistory] load failed: {ex}");
            await DisplayAlert("Error", "Could not load orders.", "OK");
        }
        finally
        {
            RefreshContainer.IsRefreshing = false;
        }
    }

    // turn our real Order model into something the XAML can bind to easily
    private static OrderDisplay ToDisplay(Order o)
    {
        // build a simple "items" line
        string itemsLine = "(no items)";
        if (o.Items != null && o.Items.Count > 0)
        {
            itemsLine = string.Join(", ",
                o.Items.Values.Select(i => $"{i.Food_Name} × {i.Quantity}"));
        }

        return new OrderDisplay
        {
            OrderDate = string.IsNullOrEmpty(o.Order_Date) ? "Unknown date" : o.Order_Date,
            CityAndSlot = $"{o.City_Name} — {o.Time_Slot}",
            ItemsSummary = itemsLine,
            Total = $"Total: {o.Total_Price:C}"
        };
    }

    // this is the shape the XAML is actually binding to
    public class OrderDisplay
    {
        public string OrderDate { get; set; } = "";
        public string CityAndSlot { get; set; } = "";
        public string ItemsSummary { get; set; } = "";
        public string Total { get; set; } = "";
    }
}
