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
            System.Diagnostics.Debug.WriteLine($"[DEBUG] GET {final} (token? {(!string.IsNullOrEmpty(token))})");
            return final;
        }

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
                // Ensure we never throw a NullRef back up to the page.
                System.Diagnostics.Debug.WriteLine($"[ERROR] GetCitiesAsync failed: {ex}");
                throw; // keep throwing so your UI shows "Error loading cities."
            }
        }
    }
}
