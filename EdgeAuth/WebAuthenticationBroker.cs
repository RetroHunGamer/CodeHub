using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace EdgeAuth
{
    /// <summary>
    /// Starts the authentication operation. You can call the methods of this class multiple times in a single application or across multiple applications at the same time.
    /// Replaces the UWP equivalent and uses the Edge browser for authentication with apps like GitHub which displays a warning running on Internet Explorer.
    /// </summary>
    public sealed class WebAuthenticationBroker
    {
        private static Uri redirectUri;
        private static ContentDialog dialog;
        private static string code = string.Empty;
        private static uint errorCode = 0;

        public static Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri)
        {
            return AuthenticateAsync(options, requestUri, Windows.Security.Authentication.Web.WebAuthenticationBroker.GetCurrentApplicationCallbackUri());
        }

        public static async Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticationOptions options, Uri requestUri, Uri callbackUri)
        {
            if (options != WebAuthenticationOptions.None)
                throw new ArgumentException("WebAuthenticationBroker currently only supports WebAuthenticationOptions.None", "options");

            redirectUri = callbackUri;
            dialog = new ContentDialog();

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new Windows.UI.Xaml.GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new Windows.UI.Xaml.GridLength(1, Windows.UI.Xaml.GridUnitType.Star) });

            var label = new TextBlock();
            label.Text = "Connecting to GitHub";
            label.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Left;
            label.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
            label.Margin = new Windows.UI.Xaml.Thickness(0);
            grid.Children.Add(label);

            var closeButton = new Button();
            closeButton.Content = "";
            closeButton.FontFamily = new FontFamily("Segoe UI Symbol");
            closeButton.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            closeButton.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
            closeButton.Margin = new Windows.UI.Xaml.Thickness(0);
            closeButton.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right;
            closeButton.Click += (s, e) => { dialog.Hide(); };
            grid.Children.Add(closeButton);
            
            var webView = new WebView(WebViewExecutionMode.SameThread) { /*Source = requestUri*/ };
           
            //Added by RetroHunGamer
            webView.Settings.IsJavaScriptEnabled = true;
            webView.Settings.IsIndexedDBEnabled = true;
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            HttpCookieManager cookieManager = filter.CookieManager;
            HttpCookieCollection cookies = cookieManager.GetCookies(requestUri);
            foreach (HttpCookie cookie in cookies) cookieManager.DeleteCookie(cookie);

            webView.AllowFocusOnInteraction = true;
            webView.SetValue(Grid.RowProperty, 1);
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.NavigationFailed += WebView_NavigationFailed;
            webView.NavigationCompleted += WebView_NavigationCompleted;
            webView.MinWidth = 480;
            webView.MinHeight = 600;
            grid.Children.Add(webView);

            dialog.Content = grid;
            //Disabled, since with it the dialog can't be closed with the 'closeButton'.
            //dialog.GotFocus += (s, e) => { webView.Focus(Windows.UI.Xaml.FocusState.Programmatic); };

            webView.Navigate(requestUri);
            var res = await dialog.ShowAsync();
            return new WebAuthenticationResult(code, errorCode, errorCode > 0 ? WebAuthenticationStatus.ErrorHttp : string.IsNullOrEmpty(code) ? WebAuthenticationStatus.UserCancel : WebAuthenticationStatus.Success);
        }
        
        private static void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            errorCode = (uint)e.WebErrorStatus;
            dialog.Hide();
        }

        private static void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.ToString().StartsWith(redirectUri.ToString()))
            {
                var querySegs = args.Uri.Query.Substring(1).Split('&');
                foreach (string seg in querySegs)
                {
                    if (seg.StartsWith("code="))
                    {
                        code = args.Uri.ToString();
                        break;
                    }
                }

                args.Cancel = true;
                dialog.Hide();
            }
        }

        private async static void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.Uri.ToString().StartsWith("https://github.com/login/oauth/"))
            {
                string function = String.Format(@"document.getElementsByClassName('js-oauth-authorize-btn btn btn-primary width-full ws-normal')[0].removeAttribute('disabled');");

                try
                {
                    await (sender as WebView).InvokeScriptAsync("eval", new string[] { function });
                    
                }
                catch (Exception)
                {

                }
            }
        }
    }
}