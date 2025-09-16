using CommunityToolkit.Maui;
using TuckBox.Data;
using System.Text.Json;
using TuckBox.Services;
using TuckBox.ViewModels;
using TuckBox.Views;   // ✅ added so Login & Register resolve

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

            // Firebase Auth
            builder.Services.AddSingleton(sp =>
            {
                using var stream = File.OpenRead(Path.Combine(FileSystem.AppDataDirectory, "appsettings.json"));
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                var apiKey = config?["FirebaseApiKey"] ?? "";

                return new FirebaseAuthService(apiKey);
            });

            builder.Services.AddSingleton(sp =>
            {
                using var stream = File.OpenRead(Path.Combine(FileSystem.AppDataDirectory, "appsettings.json"));
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                var apiKey = config?["FirebaseApiKey"] ?? "";

                System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase API Key loaded: {apiKey}");

                return new FirebaseAuthService(apiKey);
            });

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();

            // Pages
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<Register>();

            return builder.Build();
        }
    }
}
