using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace TuckBox;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    // 👇 MUST MATCH the redirectUri's scheme and host EXACTLY
    DataScheme = "com.googleusercontent.apps.971309845644-h9n09e6abha7ooa8an6g3rtgh0dotf1i",
    DataHost = "oauth2redirect"
)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
