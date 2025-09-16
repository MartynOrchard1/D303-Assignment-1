using TuckBox.Models;
using TuckBox.Data;

namespace TuckBox
{
    public partial class App : Application
    {
        private readonly AppDb _db;

        // DI constructor stays — good
        public App(AppDb db)
        {
            InitializeComponent();
            _db = db;

            // Fire-and-forget DB init + seed (doesn't block UI)
            _ = InitializeAsync();
        }

        // ✅ MAUI 9+ startup pattern
        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Do NOT call base.CreateWindow — return your own Window
            var window = new Window(new AppShell());

            // Start at Splash once Shell is attached
            window.Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await Task.Yield(); // ensure Shell.Current is available
                    if (Shell.Current is not null)
                        await Shell.Current.GoToAsync("//Splash");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Startup nav error: {ex.Message}");
                }
            });

            return window;
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _db.InitAsync();
                await SeedIfEmptyAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB init error: {ex.Message}");
            }
        }

        private async Task SeedIfEmptyAsync()
        {
            var conn = _db.Conn;

            // Cities
            var cityCount = await conn.Table<City>().CountAsync();
            if (cityCount == 0)
            {
                await conn.InsertAllAsync(new[]
                {
                    // 👇 Use property names that match your model class
                    new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Palmerston North" },
                    new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Feilding" },
                    new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Ashhurst" },
                    new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Levin" },
                });
            }

            // TimeSlots
            var slotCount = await conn.Table<TimeSlot>().CountAsync();
            if (slotCount == 0)
            {
                await conn.InsertAllAsync(new[]
                {
                    // 👇 Match your model’s property names (TimeSlot_ID vs Time_Slot_ID)
                    new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "11:45–12:15" },
                    new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "12:15–12:45" },
                    new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "12:45–1:15"  },
                    new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "1:15–1:45"   },
                });
            }
        }
    }
}
