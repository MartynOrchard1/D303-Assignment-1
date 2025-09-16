using CommunityToolkit.Maui;
using TuckBox.Data;
using System.Text.Json;
using TuckBox.Services;
using TuckBox.ViewModels;


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
           
            builder.Services.AddSingleton<AppDb>(sp =>
            {
                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "tuckbox.db3");
                return new AppDb(dbPath);
            });

            builder.Services.AddSingleton(sp =>
            {
                // Load API key from appsettings.json
                using var stream = File.OpenRead(Path.Combine(FileSystem.AppDataDirectory, "appsettings.json"));
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
                var apiKey = config?["FirebaseApiKey"] ?? "";

                return new FirebaseAuthService(apiKey);
            });

            // Register ViewModels for DI
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();

            // Register Pages so DI works with constructor injection
            builder.Services.AddTransient<Login>();
            builder.Services.AddTransient<Register>();

            return builder.Build();
        }
    }
}
