using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Services
{
    public class ImageGalleryHttpClient : IImageGalleryHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private HttpClient _httpClient = new HttpClient();

        public ImageGalleryHttpClient(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        public async Task<HttpClient> GetClient()
        {
            // QQHQ :: API
            HttpContext currentContext = this._httpContextAccessor.HttpContext;

            //// QQHQ :: API :: Get access token. IDP Call???
            //Debug.WriteLine($"QQHQ :: EXTRACT ACCESS TOKEN");
            //string accessToken = await currentContext.GetTokenAsync(
            //    OpenIdConnectParameterNames.AccessToken);

            // QQHQ :: TOKENREFRESH
            Debug.WriteLine($"QQHQ :: EXTRACT ACCESS TOKEN EXPIRATION");

            var expires_at = await currentContext.GetTokenAsync("expires_at");

            string accessToken = String.Empty;

            DateTime expireDateTime = (DateTime.Parse(expires_at).AddSeconds(-60));

            if (string.IsNullOrWhiteSpace(expires_at) ||
                (expireDateTime.ToUniversalTime() < DateTime.UtcNow))
            {
                Debug.WriteLine($"QQHQ :: ACCESS TOKEN EXPIRED :: {expireDateTime}");
                accessToken = await this.RenewTokens();
            }
            else
            {
                // Get access token
                Debug.WriteLine($"QQHQ :: EXTRACT ACCESS TOKEN");

                accessToken = await currentContext.GetTokenAsync(
                    OpenIdConnectParameterNames.AccessToken);
            }

            if (!String.IsNullOrWhiteSpace(accessToken))
            {
                // QQHQ :: API :: Set Bearer token
                Debug.WriteLine($"QQHQ :: SET BEARER ACCESS TOKEN");

                this._httpClient.SetBearerToken(accessToken);
            }

            this._httpClient.BaseAddress = new Uri("https://localhost:44329/");
            this._httpClient.DefaultRequestHeaders.Accept.Clear();
            this._httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return this._httpClient;
        }

        /// <summary>
        /// Renews the tokens using current refresh token.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">Problem encountered while refreshing tokens {discoveryResponse.TokenEndpoint}</exception>
        private async Task<string> RenewTokens()
        {
            // QQHQ :: TOKENREFRESH
            HttpClient httpClient = new HttpClient();

            // Get the metadata
            Debug.WriteLine($"QQHQ :: GET OIDC DISCOVERY");
            DiscoveryResponse discoveryResponse = await httpClient.GetDiscoveryDocumentAsync("https://localhost:44314/");

            // Get the current HttpContext to access current refresh token and set new tokens
            HttpContext currentContext = this._httpContextAccessor.HttpContext;

            // Get current refresh token
            Debug.WriteLine($"QQHQ :: EXTRACT REFRESH TOKEN");
            string currentRefreshToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);

            // Refresh the tokens
            Debug.WriteLine($"QQHQ :: RENEW TOKENS");

            TokenResponse tokenRefreshResponse = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = discoveryResponse.TokenEndpoint,
                ClientId = "imagegalleryclient",
                ClientSecret = "Client-Token-Secret",
                RefreshToken = currentRefreshToken
            });

            if (tokenRefreshResponse.IsError)
            {
                throw new Exception(
                    $"Problem encountered while refreshing tokens {discoveryResponse.TokenEndpoint} :: {tokenRefreshResponse.Error}",
                    tokenRefreshResponse.Exception);
            }

            // Create new tokens collection
            List<AuthenticationToken> updatedTokens = new List<AuthenticationToken>()
            {
                new AuthenticationToken()
                {
                    Name = OpenIdConnectParameterNames.IdToken,
                    Value = tokenRefreshResponse.IdentityToken
                },
                new AuthenticationToken()
                {
                    Name = OpenIdConnectParameterNames.AccessToken,
                    Value = tokenRefreshResponse.AccessToken
                },
                new AuthenticationToken()
                {
                    Name = OpenIdConnectParameterNames.RefreshToken,
                    Value = tokenRefreshResponse.RefreshToken
                }
            };

            // Setup expiration
            DateTime expiredAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenRefreshResponse.ExpiresIn);
            updatedTokens.Add(new AuthenticationToken()
            {
                Name = "expires_at",
                // QQHQ :: Using round-trip DateTime format to allow easy casting between string and date time or passing around the components
                Value = expiredAt.ToString("o", CultureInfo.InvariantCulture)
            });

            // QQHQ :: Extract authenticate result containing the current principal & properties from the authentication scheme holding the cookies
            AuthenticateResult currentAuthenticateResult = await currentContext.AuthenticateAsync("Cookies");

            // Store the update tokens
            currentAuthenticateResult.Properties.StoreTokens(updatedTokens);

            // QQHQ :: Sign in => actually updating the cookies
            Debug.WriteLine($"QQHQ :: SIGN IN TO UPDATE TOKENS");

            await currentContext.SignInAsync("Cookies",
                currentAuthenticateResult.Principal,
                currentAuthenticateResult.Properties);

            // Return the new access token for immediate usage after refreshing the token
            return tokenRefreshResponse.AccessToken;
        }
    }
}