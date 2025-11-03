using System;
using Microsoft.Maui.Controls;

namespace TuckBox.Views
{
    public partial class Splash : ContentPage
    {
        public Splash()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Fade in
            SplashContent.Opacity = 0;
            await SplashContent.FadeTo(1, 400, Easing.CubicIn);

            // Delay
            await Task.Delay(1500);

            // Nav to login
            await Shell.Current.GoToAsync("//Login");
        }
    }
}
