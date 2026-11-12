using System.Text.Json;
using System.Text.RegularExpressions;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AUri = Android.Net.Uri;

namespace InstaFollow.Android;

/// <summary>
/// The app is a full-screen WebView showing the same InstaFollow.html used on the PC (bundled as an asset, read
/// offline). This class only adds what a page cannot do by itself on a phone:
/// - open a profile in the Instagram app (bridge command openProfile);
/// - pick the export .zip / backup .json from the phone (the page's file input, via OnShowFileChooser);
/// - save the backup file where the user chooses (bridge command saveFile).
/// The page talks to this class through prompt("instafollow:&lt;command&gt;", JSON array of string arguments), caught in
/// OnJsPrompt. Unlike AddJavascriptInterface/[Export], that needs no reflection, so the trimmed Release build is safe.
/// The page knows it is inside the app by "InstaFollowAndroid" in the user agent.
/// </summary>
[Activity(
    Name = "io.github.brenosantin96.instafollow.MainActivity",
    Label = "@string/app_name",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTask,
    Theme = "@android:style/Theme.Material.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize | ConfigChanges.KeyboardHidden | ConfigChanges.UiMode,
    WindowSoftInputMode = SoftInput.AdjustResize)]
public class MainActivity : Activity
{
    // A fixed https origin keeps the page's localStorage (all the progress) stable across app updates.
    internal const string Host = "appassets.androidplatform.net";
    internal const string BridgePrefix = "instafollow:";
    const string StartUrl = "https://" + Host + "/index.html";
    const string InstagramPackage = "com.instagram.android";
    const int PickFilesRequest = 1;
    const int SaveFileRequest = 2;

    static readonly Regex Username = new("^[A-Za-z0-9._]{1,30}$");
    static readonly Color Background = Color.ParseColor("#0F1115");

    WebView web = null!;
    IValueCallback? pendingPick;
    string? pendingSave;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

#if DEBUG
        WebView.SetWebContentsDebuggingEnabled(true); // chrome://inspect on the PC, with the phone on USB
#endif
        web = new WebView(this);
        web.SetBackgroundColor(Background);
        var settings = web.Settings;
        settings.JavaScriptEnabled = true;
        settings.DomStorageEnabled = true;
        settings.AllowFileAccess = false;
        settings.SetSupportMultipleWindows(false);
        web.SetWebViewClient(new LocalOnlyClient(this));
        settings.UserAgentString += " InstaFollowAndroid/1";
        web.SetWebChromeClient(new AppChromeClient(this));

        // Android 15+ draws apps behind the status and navigation bars: pad the page so nothing hides under them.
        var root = new FrameLayout(this);
        root.SetBackgroundColor(Background);
        root.AddView(web, new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
        root.SetOnApplyWindowInsetsListener(new SystemBarsPadding());
        SetContentView(root);

        web.LoadUrl(StartUrl);
    }

    protected override void OnPause()
    {
        base.OnPause();
        // The WebView writes localStorage in the background; flush so the progress is on disk if Android kills the app.
        CookieManager.Instance?.Flush();
    }

    // ---------- commands from the page ----------

    internal void HandleCommand(string command, string argsJson)
    {
        string[] args;
        try { args = JsonSerializer.Deserialize(argsJson, JsonContext.Default.StringArray) ?? []; }
        catch (JsonException) { return; }
        switch (command)
        {
            case "openProfile" when args.Length >= 1:
                OpenProfile(args[0]);
                break;
            case "saveFile" when args.Length >= 2:
                SaveFile(args[0], args[1]);
                break;
        }
    }

    // ---------- opening profiles ----------

    internal void OpenProfile(string user)
    {
        if (!Username.IsMatch(user)) return;
        OpenExternal(AUri.Parse("https://www.instagram.com/" + user + "/")!);
    }

    internal void OpenExternal(AUri uri)
    {
        var intent = new Intent(Intent.ActionView, uri);
        if (uri.Host is { } host && (host == "instagram.com" || host.EndsWith(".instagram.com")))
        {
            intent.SetPackage(InstagramPackage);
            try { StartActivity(intent); return; }
            catch (ActivityNotFoundException) { intent.SetPackage(null); } // no Instagram app: use the browser
        }
        try { StartActivity(intent); }
        catch (ActivityNotFoundException) { ShowToast("Nenhum app consegue abrir " + uri); }
    }

    // ---------- picking files (the page's <input type="file">) ----------

    internal bool PickFiles(IValueCallback callback, WebChromeClient.FileChooserParams? options)
    {
        pendingPick?.OnReceiveValue(null);
        pendingPick = callback;
        var intent = new Intent(Intent.ActionGetContent);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("*/*"); // phones label .zip/.json with many different types: filtering would hide the export
        if (options?.Mode == ChromeFileChooserMode.OpenMultiple) intent.PutExtra(Intent.ExtraAllowMultiple, true);
        try
        {
            StartActivityForResult(intent, PickFilesRequest);
            return true;
        }
        catch (ActivityNotFoundException)
        {
            pendingPick = null;
            ShowToast("Nenhum app de arquivos encontrado");
            return false;
        }
    }

    // ---------- saving the backup ----------

    internal void SaveFile(string name, string text)
    {
        pendingSave = text;
        var intent = new Intent(Intent.ActionCreateDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("application/json");
        intent.PutExtra(Intent.ExtraTitle, name);
        StartActivityForResult(intent, SaveFileRequest);
    }

    protected override void OnActivityResult(int requestCode, [global::Android.Runtime.GeneratedEnum] Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode == PickFilesRequest)
        {
            var callback = pendingPick;
            pendingPick = null;
            if (callback == null) return;
            var uris = new List<AUri>();
            if (resultCode == Result.Ok && data != null)
            {
                if (data.ClipData is { } clip)
                {
                    for (var i = 0; i < clip.ItemCount; i++)
                        if (clip.GetItemAt(i)?.Uri is { } u) uris.Add(u);
                }
                else if (data.Data != null) uris.Add(data.Data);
            }
            // The WebView expects a Uri[] (or null when cancelled, so the input can be used again).
            callback.OnReceiveValue(uris.Count == 0 ? null : new global::Android.Runtime.JavaArray<AUri>(global::Android.Runtime.JNIEnv.NewArray(uris.ToArray()), global::Android.Runtime.JniHandleOwnership.TransferLocalRef));
        }
        else if (requestCode == SaveFileRequest)
        {
            var text = pendingSave;
            pendingSave = null;
            if (resultCode != Result.Ok || data?.Data == null || text == null)
            {
                NotifyPage(false, "Backup cancelado.");
                return;
            }
            try
            {
                using (var stream = ContentResolver!.OpenOutputStream(data.Data, "wt")!)
                using (var writer = new StreamWriter(stream))
                    writer.Write(text);
                NotifyPage(true, "Backup salvo.");
            }
            catch (Exception e)
            {
                NotifyPage(false, "Não consegui salvar o backup: " + e.Message);
            }
        }
    }

    void NotifyPage(bool ok, string message)
    {
        var js = "window.instafollowSaved && window.instafollowSaved(" + (ok ? "true" : "false") + "," +
                 JsonSerializer.Serialize(message, JsonContext.Default.String) + ")";
        RunOnUiThread(() => web.EvaluateJavascript(js, null));
    }

    void ShowToast(string text) =>
        RunOnUiThread(() => Toast.MakeText(this, text, ToastLength.Long)?.Show());

    // ---------- helpers ----------

    /// <summary>Serves the bundled page and refuses everything else: the page works with no network.</summary>
    sealed class LocalOnlyClient(MainActivity activity) : WebViewClient
    {
        public override WebResourceResponse? ShouldInterceptRequest(WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url;
            if (url?.Host == Host && url.Path is "/" or "/index.html")
            {
                var stream = activity.Assets!.Open("index.html");
                return new WebResourceResponse("text/html", "utf-8", stream);
            }
            return new WebResourceResponse("text/plain", "utf-8", 403, "Blocked", null, new MemoryStream());
        }

        public override bool ShouldOverrideUrlLoading(WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url;
            if (url == null || url.Host == Host) return false;
            activity.OpenExternal(url); // a link to instagram.com goes to the Instagram app, never inside this app
            return true;
        }
    }

    sealed class AppChromeClient(MainActivity activity) : WebChromeClient
    {
        public override bool OnShowFileChooser(WebView? webView, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams) =>
            filePathCallback != null && activity.PickFiles(filePathCallback, fileChooserParams);

        // prompt("instafollow:<command>", "[...args]") from the bundled page is a command; any other prompt is a real one.
        public override bool OnJsPrompt(WebView? view, string? url, string? message, string? defaultValue, JsPromptResult? result)
        {
            if (message != null && message.StartsWith(BridgePrefix, StringComparison.Ordinal) &&
                AUri.Parse(url)?.Host == Host)
            {
                result?.Confirm("ok");
                activity.HandleCommand(message[BridgePrefix.Length..], defaultValue ?? "[]");
                return true;
            }
            return base.OnJsPrompt(view, url, message, defaultValue, result);
        }
    }

    sealed class SystemBarsPadding : Java.Lang.Object, View.IOnApplyWindowInsetsListener
    {
        public WindowInsets OnApplyWindowInsets(View v, WindowInsets insets)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var i = insets.GetInsets(WindowInsets.Type.SystemBars() | WindowInsets.Type.DisplayCutout() | WindowInsets.Type.Ime());
                v.SetPadding(i.Left, i.Top, i.Right, i.Bottom);
                return WindowInsets.Consumed;
            }
#pragma warning disable CA1422 // the pre-Android 11 way, only used there
            v.SetPadding(insets.SystemWindowInsetLeft, insets.SystemWindowInsetTop, insets.SystemWindowInsetRight, insets.SystemWindowInsetBottom);
            return insets.ConsumeSystemWindowInsets();
#pragma warning restore CA1422
        }
    }
}

[System.Text.Json.Serialization.JsonSerializable(typeof(string))]
[System.Text.Json.Serialization.JsonSerializable(typeof(string[]))]
internal partial class JsonContext : System.Text.Json.Serialization.JsonSerializerContext;
