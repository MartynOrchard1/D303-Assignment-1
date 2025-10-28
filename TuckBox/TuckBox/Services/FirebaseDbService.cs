using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using TuckBox.Models;
using System.Threading.Tasks;

namespace TuckBox.Services
{
    public class FirebaseDbService
    {
        // 🔹 keep them private for internal use
        private readonly HttpClient _http;
        private readonly string _dbUrl;
        private readonly FirebaseAuthService _auth; // Token source (if used for auth tokens)

        // 🔹 expose read-only properties (safe to access externally)
        public HttpClient Http => _http;
        public string DbUrl => _dbUrl;


        public FirebaseDbService(string dbUrl, FirebaseAuthService auth)
        {
            _dbUrl = dbUrl.TrimEnd('/');
            _auth = auth;
        }

        // Write or update /Users/{uid}
        public async Task<bool> UpsertUserProfileAsync(User profile, string idToken)
        {
            var url = $"{_dbUrl}/Users/{profile.User_ID}.json?auth={idToken}";
            var json = JsonSerializer.Serialize(profile);
            var resp = await _http.PutAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] UpsertUserProfile status={resp.StatusCode} body={body}");
            return resp.IsSuccessStatusCode;
        }

        // Read /Users/{uid}
        public async Task<User?> GetUserProfileAsync(string uid, string idToken)
        {
            var url = $"{_dbUrl}/Users/{uid}.json?auth={idToken}";
            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GetUserProfile status={resp.StatusCode} body={body}");
            if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body) || body == "null")
                return null;

            return JsonSerializer.Deserialize<User>(body);
        }

        private string Url(string path)
        {
            var token = _auth.CurrentIdToken;
            System.Diagnostics.Debug.WriteLine($"[DEBUG] token present? {!string.IsNullOrEmpty(token)}");
            return string.IsNullOrEmpty(token)
                ? $"{_dbUrl}/{path}.json"                           // (will 401 under secure rules)
                : $"{_dbUrl}/{path}.json?auth={token}";
        }

        public async Task<Dictionary<string, City>> GetCitiesAsync()
        {
            var url = Url("Cities");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GET {url}");
            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Cities status={resp.StatusCode} body={body}");
            resp.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<Dictionary<string, City>>(body) ?? new();
        }

        public async Task<Dictionary<string, Food>> GetFoodsAsync()
        {
            var url = Url("Foods");
            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Foods status={resp.StatusCode} body={body}");
            resp.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<Dictionary<string, Food>>(body) ?? new();
        }

        public async Task<Dictionary<string, TimeSlot>> GetTimeSlotsAsync()
        {
            var url = Url("TimeSlots");
            var resp = await _http.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] TimeSlots status={resp.StatusCode} body={body}");
            resp.EnsureSuccessStatusCode();
            return JsonSerializer.Deserialize<Dictionary<string, TimeSlot>>(body) ?? new();
        }



    }
}

