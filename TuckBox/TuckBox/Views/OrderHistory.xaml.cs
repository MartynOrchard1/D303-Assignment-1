using TuckBox.Services;
using TuckBox.Models;

namespace TuckBox.Views;

public partial class OrderHistory : ContentPage
{
    private readonly FirebaseAuthService _auth;
    private readonly FirebaseDbService _db;

    public OrderHistory(FirebaseAuthService auth, FirebaseDbService db)
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
        OrdersList.ItemsSource = orders
            .OrderByDescending(o => o.Value.Order_Date)   // newest first
            .Select(o => o.Value)
            .ToList();
    }
}