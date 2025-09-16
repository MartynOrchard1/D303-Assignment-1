using TuckBox.Models;
using TuckBox.Data;

namespace TuckBox
{
    public partial class App : Application
    {
        private readonly AppDb _db;

        public App(AppDb db)
        {
            InitializeComponent();
            _db = db;

            // Set a quick splash or shell immediately; init runs in background
            MainPage = new AppShell();

            // fire-and-forget app initialization
            _ = InitializeAsync();
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
                // Optional: log or surface a friendly message
                System.Diagnostics.Debug.WriteLine($"DB init error: {ex.Message}");
            }
        }

        private async Task SeedIfEmptyAsync()
        {
            var conn = _db.Conn;

            // Seed Cities
            var cityCount = await conn.Table<City>().CountAsync();
            if (cityCount == 0)
            {
                await conn.InsertAllAsync(new[]
                {
                new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Palmerston North" },
                new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Feilding" },
                new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Ashhurst" },
                new City { City_ID = Guid.NewGuid().ToString(), City_Name = "Levin" },
            });
            }

            // Seed TimeSlots
            var slotCount = await conn.Table<TimeSlot>().CountAsync();
            if (slotCount == 0)
            {
                await conn.InsertAllAsync(new[]
                {
                new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "11:45–12:15" },
                new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "12:15–12:45" },
                new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "12:45–1:15"  },
                new TimeSlot { TimeSlot_ID = Guid.NewGuid().ToString(), Time_Slot = "1:15–1:45"   },
            });
            }
        }
    }
}