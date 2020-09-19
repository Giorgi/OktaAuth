using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace OktaAuth
{
    class LoginService
    {
        private string codeVerifier;

        private const string CodeChallengeMethod = "S256";

        public string BuildAuthenticationUrl()
        {
            var state = CreateCryptoGuid();
            var nonce = CreateCryptoGuid();

            var codeChallenge = CreateCodeChallenge();

            return $"{OktaConfiguration.OrganizationUrl}/oauth2/default/v1/authorize?response_type=code&scope=openid%20profile&redirect_uri={OktaConfiguration.Callback}&client_id={OktaConfiguration.ClientId}&state={state}&code_challenge={codeChallenge}&code_challenge_method={CodeChallengeMethod}&nonce={nonce}";
        }

        public async Task<UserToken> ExchangeCodeForIdToken(WebAuthenticatorResult authenticatorResult)
        {
            using (var httpClient = new HttpClient { BaseAddress = new Uri($"{OktaConfiguration.OrganizationUrl}/oauth2/default/v1/") })
            {
                var data = new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", OktaConfiguration.ClientId },
                    { "redirect_uri", OktaConfiguration.Callback },
                    { "code_verifier", codeVerifier },
                    { "code", authenticatorResult.Properties["code"] }
                };

                var responseMessage = await httpClient.PostAsync("token", new FormUrlEncodedContent(data));
                var response = await responseMessage.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserToken>(response);
            }
        }

        public string BuildLogOutUrl(string idToken)
        {
            return $"{OktaConfiguration.OrganizationUrl}/oauth2/default/v1/logout?post_logout_redirect_uri={OktaConfiguration.LogOutCallback}&client_id={OktaConfiguration.ClientId}&id_token_hint={idToken}";
        }

        private string CreateCryptoGuid()
        {
            using (var generator = RandomNumberGenerator.Create())
            {
                var bytes = new byte[16];
                generator.GetBytes(bytes);

                return new Guid(bytes).ToString("N");
            }
        }

        private string CreateCodeChallenge()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);

                codeVerifier = Base64UrlEncoder.Encode(bytes);

                using (var sha256 = SHA256.Create())
                {
                    var codeChallengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                    return Base64UrlEncoder.Encode(codeChallengeBytes);
                }
            }
        }

        public JwtSecurityToken ParseAuthenticationResult(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ReadJwtToken(token);
        }
    }

    public class UserToken
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}