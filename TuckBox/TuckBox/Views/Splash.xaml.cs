using System;
using System.Threading.Tasks;

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
        // Fade/scale in
        await Task.WhenAll(
            Logo.FadeTo(1, 500, Easing.CubicOut),
            Logo.ScaleTo(1.0, 500, Easing.CubicOut)
        );

        // Show the label after logo
        if (this.FindByName<Label>("Label") is Label label)
        {
            await label.FadeTo(1, 300, Easing.CubicOut);
        }

        // Hold for a moment
        await Task.Delay(900);

        // Fade out
        await Logo.FadeTo(0, 350);
        await Task.Delay(150);

        // Navigate to Login
        await Shell.Current.GoToAsync("//Login");
    }
}