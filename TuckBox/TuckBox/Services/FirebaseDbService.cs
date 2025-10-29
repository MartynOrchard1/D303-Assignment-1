using System.Text;
using System.Text.Json;
using TuckBox.Models;

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
        public async Task<bool> PlaceOrderAsync(string uid, Order order)
        {
            try
            {
                var json = JsonSerializer.Serialize(order);
                var resp = await _http.PutAsync(
                    BuildUrl($"Orders/{uid}/{order.Order_ID}"),
                    new StringContent(json, Encoding.UTF8, "application/json")
                );
                var body = await resp.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] PlaceOrder status={resp.StatusCode} body={body}");
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] PlaceOrderAsync failed: {ex.Message}");
                return false;
            }
        }
    }
}
