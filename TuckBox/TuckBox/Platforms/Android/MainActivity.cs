using Android.App;
using Android.Content.PM;
using Android.OS;


namespace TuckBox;

[Activity(Label = "TuckBox", Theme = "@style/Maui.SplashTheme", MainLauncher = true,
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                                 ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
// 👇 Add this block (replace scheme with *your* GoogleRedirectUri scheme)
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = "com.googleusercontent.apps.1234567890-abcdefg", // <- from your appsettings
    DataHost = "oauth2redirect")]
public class MainActivity : MauiAppCompatActivity
{
}