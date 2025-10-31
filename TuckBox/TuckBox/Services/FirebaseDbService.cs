using System.Text;
using System.Text.Json;
using TuckBox.Models;
using System.Linq;

namespace TuckBox.Services
{
    public class FirebaseDbService
    {
        private readonly string _dbUrl;
        private readonly HttpClient _http;
        private readonly FirebaseAuthService? _auth; // optional (public-rules mode supported)


        // Preferred: secure rules (auth != null)
        public FirebaseDbService(string dbUrl, FirebaseAuthService auth)
        {
            _dbUrl = dbUrl.TrimEnd('/');
            _http = new HttpClient();
            _auth = auth;
        }

        // Fallback: public-rules mode (no token)
        public FirebaseDbService(string dbUrl)
        {
            _dbUrl = dbUrl.TrimEnd('/');
            _http = new HttpClient();
            _auth = null;
        }

        // Build URL; append ?auth= token only if we have one
        private string BuildUrl(string path)
        {
            var baseUrl = $"{_dbUrl}/{path}.json";
            var token = _auth?.CurrentIdToken;
            var final = !string.IsNullOrEmpty(token) ? $"{baseUrl}?auth={token}" : baseUrl;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] URL {final} (token? {(!string.IsNullOrEmpty(token))})");
            return final;
        }

        // -------- Cities --------
        public async Task<Dictionary<string, City>> GetCitiesAsync()
        {
            try
            {
                var resp = await _http.GetAsync(BuildUrl("Cities"));
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Cities status={resp.StatusCode} body={body}");

                resp.EnsureSuccessStatusCode();
                var dict = JsonSerializer.Deserialize<Dictionary<string, City>>(body);
                return dict ?? new Dictionary<string, City>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetCitiesAsync failed: {ex}");
                throw; // let UI show "Error loading cities."
            }
        }

        // -------- TimeSlots --------
        public async Task<Dictionary<string, TimeSlot>> GetTimeSlotsAsync()
        {
            try
            {
                var resp = await _http.GetAsync(BuildUrl("TimeSlots"));
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] TimeSlots status={resp.StatusCode} body={body}");
                resp.EnsureSuccessStatusCode();

                return JsonSerializer.Deserialize<Dictionary<string, TimeSlot>>(body) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetTimeSlotsAsync failed: {ex.Message}");
                return new();
            }
        }

        // -------- Foods --------
        public async Task<Dictionary<string, Food>> GetFoodsAsync()
        {
            try
            {
                var resp = await _http.GetAsync(BuildUrl("Foods"));
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Foods status={resp.StatusCode} body={body}");
                resp.EnsureSuccessStatusCode();

                return JsonSerializer.Deserialize<Dictionary<string, Food>>(body) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetFoodsAsync failed: {ex.Message}");
                return new();
            }
        }

        // Get orders
        public async Task<Dictionary<string, Order>> GetOrdersForUserAsync(string userId)
        {
            // get everything under /Orders
            var resp = await _http.GetAsync(BuildUrl("Orders"));
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GetOrdersForUser status={resp.StatusCode} body={body}");

            if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body) || body == "null")
                return new();

            var all = JsonSerializer.Deserialize<Dictionary<string, Order>>(body) ?? new();

            // filter to just this user
            var mine = all
                .Where(kvp => kvp.Value != null && kvp.Value.User_ID == userId)
                .ToDictionary(k => k.Key, v => v.Value);

            return mine;
        }

        // -------- User Profile (Users/{uid}) --------
        public async Task<bool> UpsertUserProfileAsync(Models.User profile, string idToken)
        {
            if (string.IsNullOrEmpty(idToken)) throw new InvalidOperationException("Missing ID token");
            var url = $"{_dbUrl}/Users/{profile.User_ID}.json?auth={idToken}";
            var json = JsonSerializer.Serialize(profile);

            var resp = await _http.PutAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] UpsertUserProfile status={resp.StatusCode} body={body}");
            return resp.IsSuccessStatusCode;
        }

        public async Task<Models.User?> GetUserProfileAsync(string uid, string idToken)
        {
            if (string.IsNullOrEmpty(idToken)) throw new InvalidOperationException("Missing ID token");
            var url = $"{_dbUrl}/Users/{uid}.json?auth={idToken}";

            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GetUserProfile status={resp.StatusCode} body={body}");

            if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body) || body == "null")
                return null;

            return JsonSerializer.Deserialize<Models.User>(body);
        }

        // -------- Delivery Addresses (DeliveryAddresses/{uid}/{addressId}) --------
        public async Task<Dictionary<string, DeliveryAddress>> GetUserAddressesAsync(string uid)
        {
            try
            {
                var resp = await _http.GetAsync(BuildUrl($"DeliveryAddresses/{uid}"));
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Addresses status={resp.StatusCode} body={body}");

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body) || body == "null")
                    return new();

                return JsonSerializer.Deserialize<Dictionary<string, DeliveryAddress>>(body) ?? new();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetUserAddressesAsync failed: {ex.Message}");
                return new();
            }
        }

        public async Task<bool> UpsertUserAddressAsync(string uid, DeliveryAddress addr)
        {
            try
            {
                var json = JsonSerializer.Serialize(addr);
                var resp = await _http.PutAsync(
                    BuildUrl($"DeliveryAddresses/{uid}/{addr.Address_ID}"),
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UpsertAddress status={resp.StatusCode} body={body}");
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] UpsertUserAddressAsync failed: {ex.Message}");
                return false;
            }
        }

        // -------- Orders (Orders/{uid}/{orderId}) --------

            public async Task<bool> PlaceOrderAsync(
                string userId,
                City city,
                TimeSlot slot,
                DeliveryAddress address,
                List<(Food food, int qty, string? option)> items)
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentException("userId is required");

                // 1) make a single order id
                var orderId = $"ord-{Guid.NewGuid():N}".Substring(0, 12);

                // 2) NZ time
                var nzZone = TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time");
                var nowNz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, nzZone);
                var formattedNz = nowNz.ToString("dd/MM/yyyy HH:mm:ss");

                // 3) build items
                var itemsDict = new Dictionary<string, object>();
                decimal total = 0m;

                foreach (var (food, qty, opt) in items)
                {
                    if (qty <= 0) continue;

                    var lineTotal = (decimal)food.Price * qty;
                    total += lineTotal;

                    itemsDict[food.Food_ID] = new
                    {
                        Food_ID = food.Food_ID,
                        Food_Name = food.Food_Name,
                        Quantity = qty,
                        Option_Key = food.Option_Key,
                        Option_Value = opt ?? "",
                        Unit_Price = food.Price,
                        Line_Total = lineTotal
                    };
                }

                if (itemsDict.Count == 0)
                    return false; // nothing to save

                // 4) final order payload
                var orderPayload = new
                {
                    Order_ID = orderId,
                    Order_Date = formattedNz,
                    City_ID = city.City_ID,
                    City_Name = city.City_Name,
                    Time_Slot_ID = slot.TimeSlot_ID,
                    Time_Slot = slot.Time_Slot,
                    Address_ID = address.Address_ID,
                    Address = address.Address,
                    User_ID = userId,
                    Total_Price = total,
                    Items = itemsDict
                };

                // 5) POST/PUT to firebase
                // /Orders/{orderId}.json?auth=...
                var url = BuildUrl($"Orders/{orderId}");
                var json = JsonSerializer.Serialize(orderPayload);
                var resp = await _http.PutAsync(
                    url,
                    new StringContent(json, Encoding.UTF8, "application/json"));

                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] PlaceOrderAsync status={resp.StatusCode} body={body}");

                return resp.IsSuccessStatusCode;
            }
        }
    }
