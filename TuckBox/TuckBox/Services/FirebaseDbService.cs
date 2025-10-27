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

        public FirebaseDbService(string dbUrl)
        {
            _dbUrl = dbUrl.TrimEnd('/');
        }

        // Quick test method to verify Firebase connection
        public async Task<Dictionary<string, City>> GetCitiesAsync()
        {
            var resp = await _http.GetAsync($"{_dbUrl}/Cities.json");
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync();
            var cities = JsonSerializer.Deserialize<Dictionary<string, City>>(json);

            return cities ?? new();
        }
    }
}

