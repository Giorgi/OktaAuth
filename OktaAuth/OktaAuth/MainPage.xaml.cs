using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OktaAuth
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private readonly LoginService loginService = new LoginService();
        private UserToken userToken;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void LoginButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var callbackUrl = new Uri(OktaConfiguration.Callback);
                var loginUrl = new Uri(loginService.BuildAuthenticationUrl());
                
                var authenticatorResult = await WebAuthenticator.AuthenticateAsync(loginUrl, callbackUrl);
                
                userToken = await loginService.ExchangeCodeForIdToken(authenticatorResult);
                var idToken = loginService.ParseAuthenticationResult(userToken.IdToken);
                
                var nameClaim = idToken.Claims.FirstOrDefault(claim => claim.Type == "name");

                if (nameClaim != null)
                {
                    WelcomeLabel.Text = $"Welcome to Xamarin.Forms {nameClaim.Value}!";
                    LogoutButton.IsVisible = !(LoginButton.IsVisible = false);
                }
            }
            catch (TaskCanceledException)
            {

            }
        }

        private async void LogoutButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var callbackUrl = new Uri(OktaConfiguration.LogOutCallback);
                var buildLogOutUrl = loginService.BuildLogOutUrl(userToken.IdToken);
                var logoutResult = await WebAuthenticator.AuthenticateAsync(new Uri(buildLogOutUrl), callbackUrl);
                userToken = null;

                WelcomeLabel.Text = "Welcome to Xamarin.Forms!";
                LogoutButton.IsVisible = !(LoginButton.IsVisible = true);
            }
            catch (TaskCanceledException)
            {

            }
        }
    }
}
