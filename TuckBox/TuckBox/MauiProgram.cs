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

            // ✅ Load configuration ONCE from appsettings.json
            var cfg = LoadConfig();
            var apiKey = cfg["FirebaseApiKey"];
            var dbUrl = cfg["FirebaseDbUrl"];
            var googleClientId = cfg["GoogleClientId"];
            var googleRedirectUri = cfg["GoogleRedirectUri"];

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase API Key: {apiKey}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase DB URL: {dbUrl}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GoogleClientId: {googleClientId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GoogleRedirectUri: {googleRedirectUri}");

            // ✅ Local SQLite Database
            builder.Services.AddSingleton<AppDb>(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tuckbox.db3");
                return new AppDb(dbPath);
            });

            // ✅ Firebase Authentication Service (singleton)
            builder.Services.AddSingleton(new FirebaseAuthService(apiKey));

            // ✅ Firebase Realtime Database Service (public access for now)
            //   → Uses only DB URL; no auth token required
            builder.Services.AddSingleton(new FirebaseDbService(dbUrl));

            // ✅ ViewModels
            builder.Services.AddTransient<LoginViewModel>(sp =>
            {
                var auth = sp.GetRequiredService<FirebaseAuthService>();
                return new LoginViewModel(auth, googleClientId, googleRedirectUri);
            });

            builder.Services.AddTransient<RegisterViewModel>();

            // ✅ Pages
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<Register>();

            return builder.Build();
        }

        // ✅ Helper to load JSON config only once
        private static Dictionary<string, string> LoadConfig()
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").Result;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
        }
    }
}
