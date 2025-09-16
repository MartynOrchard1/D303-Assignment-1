using CommunityToolkit.Maui;
using TuckBox.Data;
using System.Text.Json;
using TuckBox.Services;
using TuckBox.ViewModels;
using TuckBox.Views;

namespace TuckBox
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Database
            builder.Services.AddSingleton<AppDb>(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tuckbox.db3");
                return new AppDb(dbPath);
            });

            // Firebase Auth service (with API key)
            builder.Services.AddSingleton(sp =>
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

                var apiKey = cfg["FirebaseApiKey"];
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase API Key loaded: {apiKey}");

                return new FirebaseAuthService(apiKey);
            });

            // Login ViewModel (needs Google client/redirect)
            builder.Services.AddTransient<LoginViewModel>(sp =>
            {
                using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] appsettings.json content: {json}");

                var cfg = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;

                // Safer reads with helpful errors:
                if (!cfg.TryGetValue("GoogleClientId", out var googleClientId))
                    throw new Exception("GoogleClientId missing from appsettings.json in package");
                if (!cfg.TryGetValue("GoogleRedirectUri", out var googleRedirectUri))
                    throw new Exception("GoogleRedirectUri missing from appsettings.json in package");

                var auth = sp.GetRequiredService<FirebaseAuthService>();
                return new LoginViewModel(auth, googleClientId, googleRedirectUri);

            });

            // Register VM
            builder.Services.AddTransient<RegisterViewModel>();

            // Pages
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<Register>();

            return builder.Build();
        }
    }
}
