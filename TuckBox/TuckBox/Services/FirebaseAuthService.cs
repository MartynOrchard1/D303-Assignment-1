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

    // ✅ ADDED: expose the current Firebase ID token + UID for REST calls (e.g., Realtime DB ?auth=ID_TOKEN)
    public string? CurrentIdToken { get; private set; }   // Firebase ID token (JWT)
    public string? CurrentUserId { get; private set; }   // Firebase UID (localId)

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

            // ✅ ADDED: capture ID token + UID
            CurrentIdToken = doc.RootElement.GetProperty("idToken").GetString();
            CurrentUserId = doc.RootElement.GetProperty("localId").GetString();

            return CurrentUserId;
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

            // ✅ ADDED: capture ID token + UID
            CurrentIdToken = doc.RootElement.GetProperty("idToken").GetString();
            CurrentUserId = doc.RootElement.GetProperty("localId").GetString();

            return CurrentUserId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SignIn exception: {ex}");
            return null;
        }
    }

    public async Task<string?> SignInWithGoogleAsync(
        string googleClientId,
        string authRedirectUriHttps,   // e.g. https://MartynOrchard1.github.io/
        string appCallbackUriCustom    // e.g. com.google...:/oauth2redirect
    )
    {
        try
        {
            // PKCE setup
            var codeVerifier = MakeCodeVerifier();
            var codeChallenge = MakeCodeChallenge(codeVerifier);
            var scope = "openid email profile";

            var authorizeUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                $"?client_id={Uri.EscapeDataString(googleClientId)}" +
                $"&redirect_uri={Uri.EscapeDataString(authRedirectUriHttps)}" + // Google only allows HTTPS
                $"&response_type=code" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                $"&code_challenge_method=S256" +
                $"&prompt=select_account";

            var authUrl = new Uri(authorizeUrl);
            var callbackUrl = new Uri(appCallbackUriCustom);

            System.Diagnostics.Debug.WriteLine($"[DEBUG] Using client_id: {googleClientId}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Auth redirect (HTTPS): {authRedirectUriHttps}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] App callback (custom): {appCallbackUriCustom}");
            System.Diagnostics.Debug.WriteLine("[DEBUG] Calling AuthenticateAsync...");

            var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

            System.Diagnostics.Debug.WriteLine("[DEBUG] AuthenticateAsync returned.");
            foreach (var kv in result.Properties)
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Callback prop: {kv.Key}={kv.Value}");

            // Try to read the authorization code
            string? authCode = null;
            result.Properties.TryGetValue("code", out authCode);

            // 🔸 Fallback: some providers return an id_token directly instead of code
            if (string.IsNullOrEmpty(authCode) && result.Properties.TryGetValue("id_token", out var directIdToken))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] No 'code' found; got 'id_token' directly – using implicit flow fallback.");

                var fbRespFallback = await _http.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}",
                    new
                    {
                        postBody = $"id_token={directIdToken}&providerId=google.com",
                        requestUri = "http://localhost",
                        returnSecureToken = true
                    });

                var fbBodyFallback = await fbRespFallback.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase (implicit) status={fbRespFallback.StatusCode} body={fbBodyFallback}");

                if (!fbRespFallback.IsSuccessStatusCode) return null;

                var fbDoc = JsonDocument.Parse(fbBodyFallback);

                // ✅ ADDED: capture ID token + UID after signInWithIdp
                CurrentUserId = fbDoc.RootElement.GetProperty("localId").GetString();
                CurrentIdToken = fbDoc.RootElement.GetProperty("idToken").GetString();

                return CurrentUserId;
            }

            if (string.IsNullOrEmpty(authCode))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Neither 'code' nor 'id_token' present. Check redirect URIs.");
                return null;
            }

            // Exchange code for tokens at Google
            var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authCode,
                    ["client_id"] = googleClientId,
                    ["code_verifier"] = codeVerifier,
                    ["redirect_uri"] = authRedirectUriHttps,  // must match exactly what was sent in auth step
                    ["grant_type"] = "authorization_code"
                })
            };

            var tokenResp = await _http.SendAsync(tokenReq);
            var tokenBody = await tokenResp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google token status={tokenResp.StatusCode} body={tokenBody}");

            if (!tokenResp.IsSuccessStatusCode) return null;

            var tokenDoc = JsonDocument.Parse(tokenBody);
            var idToken = tokenDoc.RootElement.GetProperty("id_token").GetString();
            if (string.IsNullOrEmpty(idToken))
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] Google token exchange returned no id_token.");
                return null;
            }

            // Exchange Google id_token with Firebase
            var fbResp = await _http.PostAsJsonAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={_apiKey}",
                new
                {
                    postBody = $"id_token={idToken}&providerId=google.com",
                    requestUri = "http://localhost",
                    returnSecureToken = true
                });

            var fbBody = await fbResp.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Firebase signInWithIdp status={fbResp.StatusCode} body={fbBody}");

            if (!fbResp.IsSuccessStatusCode) return null;

            var fbDoc2 = JsonDocument.Parse(fbBody);

            // ✅ ADDED: capture ID token + UID after signInWithIdp
            CurrentUserId = fbDoc2.RootElement.GetProperty("localId").GetString();
            CurrentIdToken = fbDoc2.RootElement.GetProperty("idToken").GetString();

            return CurrentUserId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Google SignIn (PKCE) exception: {ex}");
            return null;
        }
    }

    // (Optional) ✅ ADDED: simple sign-out helper so you can clear tokens when logging out
    public void SignOut()
    {
        CurrentIdToken = null;
        CurrentUserId = null;
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
