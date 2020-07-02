using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Xamarin.Essentials;

namespace OktaAuth
{
    class LoginService
    {
        private string codeVerifier;

        private const string IDToken = "id_token";
        private const string CodeChallengeMethod = "S256";

        public string BuildAuthenticationUrl()
        {
            var state = CreateCryptoGuid();
            var nonce = CreateCryptoGuid();
            
            var codeChallenge = CreateCodeChallenge();

            return $"{OktaConfiguration.OrganizationUrl}/oauth2/default/v1/authorize?response_type={IDToken}&scope=openid%20profile&redirect_uri={OktaConfiguration.Callback}&client_id={OktaConfiguration.ClientId}&state={state}&code_challenge={codeChallenge}&code_challenge_method={CodeChallengeMethod}&nonce={nonce}";
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
            codeVerifier = CreateCryptoGuid();
            using (var sha256 = SHA256.Create())
            {
                var codeChallengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));

                return Convert.ToBase64String(codeChallengeBytes);
            }
        }

        public JwtSecurityToken ParseAuthenticationResult(WebAuthenticatorResult authenticationResult)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(authenticationResult.IdToken);
            return token;
        }
    }
}