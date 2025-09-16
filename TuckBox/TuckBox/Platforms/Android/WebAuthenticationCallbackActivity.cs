using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Maui.Authentication;

namespace TuckBox;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "com.googleusercontent.apps.971309845644-bsbjg2sbr4k6is8mivmq1h1g9nval02p",
    DataPath = "/oauth2redirect"
)]
public class WebAuthenticationCallbackActivity : WebAuthenticatorCallbackActivity
{
}
