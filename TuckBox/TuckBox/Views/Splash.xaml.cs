namespace TuckBox.Views;

public partial class Splash : ContentPage
{
    public Splash()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        // Fade in logo + title
        await Task.WhenAll(
            Logo.FadeTo(1, 500, Easing.CubicOut)
        );

        // Pause (how long splash stays visible)
        await Task.Delay(2000); // 2 seconds

        // Optional fade out before navigating
        await Task.WhenAll(
            Logo.FadeTo(0, 500)
        );

        // Navigate to Login
        await Shell.Current.GoToAsync("//Login");
    }


}
