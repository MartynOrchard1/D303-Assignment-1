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

            // simple fade in
            SplashContent.Opacity = 0;
            await SplashContent.FadeTo(1, 400, Easing.CubicIn);

            // keep your 2s-ish delay
            await Task.Delay(1500);

            // then go to login (or main if logged in)
            await Shell.Current.GoToAsync("//Login");
        }
    }
}
