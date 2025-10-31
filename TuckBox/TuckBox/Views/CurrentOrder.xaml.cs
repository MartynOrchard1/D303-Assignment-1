using TuckBox.Services;
using TuckBox.Models;

namespace TuckBox.Views;

public partial class CurrentOrder : ContentPage
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _db;

    public CurrentOrder(FirebaseAuthService auth, FirebaseDbService db)
    {
        InitializeComponent();
        _auth = auth;
        _db = db;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (string.IsNullOrEmpty(_auth.CurrentUserId))
        {
            await DisplayAlert("Not signed in", "Please sign in again.", "OK");
            await Shell.Current.GoToAsync("//Login");
            return;
        }

        var orders = await _db.GetOrdersForUserAsync(_auth.CurrentUserId);
        if (orders.Count == 0)
        {
            StatusLabel.Text = "No current order.";
            return;
        }

        // pick latest by date string (we saved dd/MM/yyyy HH:mm:ss)
        var latest = orders
            .Select(kv => kv.Value)
            .OrderByDescending(o => o.Order_Date)
            .First();

        StatusLabel.Text = $"Last order: {latest.Order_Date}\n" +
                           $"City: {latest.City_Name}\n" +
                           $"Time: {latest.Time_Slot}\n" +
                           $"Total: {latest.Total_Price:C}";

        // show items if you want
        ItemsList.ItemsSource = latest.Items?.Values?.ToList();
    }
}