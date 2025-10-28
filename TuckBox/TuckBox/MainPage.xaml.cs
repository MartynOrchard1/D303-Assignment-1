namespace TuckBox;
using TuckBox.Models;
using TuckBox.Services;
using System.Collections.ObjectModel;

public partial class MainPage : ContentPage
{
    private readonly FirebaseDbService _db;
    private readonly ObservableCollection<City> _cities = new();

    public MainPage(FirebaseDbService dbService)
    {
        InitializeComponent();
        _db = dbService;
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
}
