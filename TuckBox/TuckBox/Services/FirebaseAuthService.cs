using System.Net.Http.Json;
using System.Text.Json;

namespace TuckBox.Services;

public class FirebaseAuthService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public FirebaseAuthService(string apiKey)
    {
        _http = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<string?> SignUpAsync(string email, string password)
    {
        var payload = new { email, password, returnSecureToken = true };

        var resp = await _http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}", payload);

        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("localId").GetString();
    }

    public async Task<string?> SignInAsync(string email, string password)
    {
        var payload = new { email, password, returnSecureToken = true };

        var resp = await _http.PostAsJsonAsync(
            $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}", payload);

        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("localId").GetString();
    }
}
