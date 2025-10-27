namespace TuckBox;
using TuckBox.Services;

public partial class MainPage : ContentPage
{

    private readonly FirebaseDbService _dbService;

    public MainPage(FirebaseDbService dbService)
    {
        InitializeComponent();
        _dbService = dbService;

        LoadCities();
    }

    private async void LoadCities()
    {
        try
        {
            var cities = await _dbService.GetCitiesAsync();
            foreach (var city in cities.Values)
                System.Diagnostics.Debug.WriteLine($"City: {city.City_Name}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] Firebase fetch failed: {ex.Message}");
        }
    }
}
