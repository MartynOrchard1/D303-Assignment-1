using CommunityToolkit.Maui;
using TuckBox.Data;


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

            return builder.Build();
        }
    }
}
