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
        private readonly HttpClient _http = new HttpClient();
        private readonly string _dbUrl;
        private readonly FirebaseAuthService _auth; // Token source

        public FirebaseDbService(string dbUrl, FirebaseAuthService auth)
        {
            _dbUrl = dbUrl.TrimEnd('/');
            _auth = auth;
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

