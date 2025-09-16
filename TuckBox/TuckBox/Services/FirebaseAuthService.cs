using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Authentication; // WebAuthenticator
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

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
            // 1) PKCE: make a code_verifier and code_challenge (S256)
            var codeVerifier = MakeCodeVerifier();
            var codeChallenge = MakeCodeChallenge(codeVerifier);

            var scope = "openid email profile";
            var authorizeUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(googleClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256" +
                $"&prompt=select_account";

            var authUrl = new Uri(authorizeUrl);
            var callbackUrl = new Uri(redirectUri);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Using client_id: {googleClientId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Using redirectUri: {redirectUri}");

            // 2) Get authorization code from Google
            var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

            string? authCode = null;
            if (!result.Properties.TryGetValue("code", out authCode) || string.IsNullOrEmpty(authCode))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Google Auth: missing 'code' in result.");
                return null;
            }

            // 3) Exchange code for tokens at Google (with PKCE verifier)
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authCode,
                    ["client_id"] = googleClientId,
                    ["code_verifier"] = codeVerifier,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                })
            };

            var tokenResp = await _http.SendAsync(tokenReq);
            var tokenBody = await tokenResp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google token status={tokenResp.StatusCode} body={tokenBody}");

            if (!tokenResp.IsSuccessStatusCode)
                return null;

            var tokenDoc = JsonDocument.Parse(tokenBody);
            if (!tokenDoc.RootElement.TryGetProperty("id_token", out var idTokenEl))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Google token: no id_token");
                return null;
            }

            var idToken = idTokenEl.GetString();
            if (string.IsNullOrEmpty(idToken))
                return null;

            // 4) Exchange Google id_token with Firebase
            var payload = new
            {
                postBody = $"id_token={idToken}&providerId=google.com",
                requestUri = "http://localhost",  // required placeholder
                returnSecureToken = true
            };

            var fbResp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}", payload);

            var fbBody = await fbResp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase signInWithIdp status={fbResp.StatusCode} body={fbBody}");

            if (!fbResp.IsSuccessStatusCode)
                return null;

            var fbDoc = JsonDocument.Parse(fbBody);
            return fbDoc.RootElement.GetProperty("localId").GetString(); // Firebase UID
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google SignIn (PKCE) exception: {ex}");
            return null;
        }
    }

    // ---- Helpers for PKCE ----
    private static string MakeCodeVerifier()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64Url(bytes);
    }

    private static string MakeCodeChallenge(string verifier)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    private static string Base64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
