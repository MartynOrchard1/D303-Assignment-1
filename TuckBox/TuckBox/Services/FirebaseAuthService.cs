using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Authentication; // WebAuthenticator

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

    // Email/Password sign-up -> returns Firebase UID (localId) or null on failure
    public async Task<string?> SignUpAsync(string email, string password)
    {
        try
        {
            var payload = new { email, password, returnSecureToken = true };
            var resp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}", payload);

            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SignUp status={resp.StatusCode} body={body}");

            if (!resp.IsSuccessStatusCode) return null;

            var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("localId").GetString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SignUp exception: {ex}");
            return null;
        }
    }

    // Email/Password sign-in -> returns Firebase UID (localId) or null on failure
    public async Task<string?> SignInAsync(string email, string password)
    {
        try
        {
            var payload = new { email, password, returnSecureToken = true };
            var resp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}", payload);

            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SignIn status={resp.StatusCode} body={body}");

            if (!resp.IsSuccessStatusCode) return null;

            var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("localId").GetString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SignIn exception: {ex}");
            return null;
        }
    }

    // Google sign-in via WebAuthenticator, then exchange with Firebase
    public async Task<string?> SignInWithGoogleAsync(string googleClientId, string redirectUri)
    {
        try
        {
            var nonce = Guid.NewGuid().ToString("N");
            var scope = "openid%20email%20profile";

            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={googleClientId}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_type=id_token" +     // request an ID token directly
                $"&scope={scope}" +
                $"&nonce={nonce}"
            );

            var callbackUrl = new Uri(redirectUri);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Using redirectUri: {redirectUri}");

            var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

            // Grab the ID token from result
            string? idToken = null;
            if (result.Properties.TryGetValue("id_token", out var idt)) idToken = idt;
#if NET9_0_OR_GREATER
            if (string.IsNullOrEmpty(idToken)) idToken = result.IdToken; // some platforms expose it here
#endif
            if (string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Google sign-in: missing id_token");
                return null;
            }

            // Exchange the Google ID token with Firebase
            var payload = new
            {
                postBody = $"id_token={idToken}&providerId=google.com",
                requestUri = "http://localhost",   // required, any valid URL
                returnSecureToken = true
            };

            var resp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}", payload);

            var body = await resp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] signInWithIdp status={resp.StatusCode} body={body}");

            if (!resp.IsSuccessStatusCode) return null;

            var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("localId").GetString(); // Firebase UID
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google SignIn exception: {ex}");
            return null;
        }
    }
}
