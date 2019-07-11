using ImageGallery.Client.Services;
using ImageGallery.Client.ViewModels;
using ImageGallery.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace ImageGallery.Client.Controllers
{
    [Authorize] // QQHQ :: SERVER :: Only authenticated user
    public class GalleryController : Controller
    {
        private readonly IImageGalleryHttpClient _imageGalleryHttpClient;

        public GalleryController(IImageGalleryHttpClient imageGalleryHttpClient)
        {
            this._imageGalleryHttpClient = imageGalleryHttpClient;
        }

        public async Task<IActionResult> Index()
        {
            await this.WriteOutIdentityInformation();

            // call the API
            Debug.WriteLine($"QQHQ :: ...*CALLING API");

            HttpClient httpClient = await this._imageGalleryHttpClient.GetClient();

            HttpResponseMessage response = await httpClient.GetAsync("api/images").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                string imagesAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                GalleryIndexViewModel galleryIndexViewModel = new GalleryIndexViewModel(
                    JsonConvert.DeserializeObject<IList<Image>>(imagesAsString).ToList());

                Debug.WriteLine($"QQHQ :: ...*RENDERING HOME PAGE");

                return this.View(galleryIndexViewModel);
            }

            // QQHQ :: API
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return this.RedirectToAction("AccessDenied", "Authorization");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> EditImage(Guid id)
        {
            await this.WriteOutIdentityInformation();

            // call the API
            Debug.WriteLine($"QQHQ :: ...*CALLING API");

            HttpClient httpClient = await this._imageGalleryHttpClient.GetClient();

            HttpResponseMessage response = await httpClient.GetAsync($"api/images/{id}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                string imageAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Image deserializedImage = JsonConvert.DeserializeObject<Image>(imageAsString);

                EditImageViewModel editImageViewModel = new EditImageViewModel()
                {
                    Id = deserializedImage.Id,
                    Title = deserializedImage.Title
                };

                Debug.WriteLine($"QQHQ :: ...*RENDERING EDIT PAGE");

                return this.View(editImageViewModel);
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditImage(EditImageViewModel editImageViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View();
            }

            // create an ImageForUpdate instance
            ImageForUpdate imageForUpdate = new ImageForUpdate()
                { Title = editImageViewModel.Title };

            // serialize it
            string serializedImageForUpdate = JsonConvert.SerializeObject(imageForUpdate);

            // call the API
            Debug.WriteLine($"QQHQ :: ...*CALLING API");

            HttpClient httpClient = await this._imageGalleryHttpClient.GetClient();

            HttpResponseMessage response = await httpClient.PutAsync(
                    $"api/images/{editImageViewModel.Id}",
                    new StringContent(serializedImageForUpdate, Encoding.Unicode, "application/json"))
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return this.RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task<IActionResult> DeleteImage(Guid id)
        {
            // call the API
            Debug.WriteLine($"QQHQ :: ...*CALLING API");

            HttpClient httpClient = await this._imageGalleryHttpClient.GetClient();

            HttpResponseMessage response = await httpClient.DeleteAsync($"api/images/{id}").ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return this.RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public IActionResult AddImage()
        {
            return this.View();
        }

        [Authorize(Roles = "PayingUser")] // QQHQ :: ROLES :: Only user in the specific role(s) can access such action even outside of the form access point (e.g. copy link and paste)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImage(AddImageViewModel addImageViewModel)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View();
            }

            // create an ImageForCreation instance
            ImageForCreation imageForCreation = new ImageForCreation()
                { Title = addImageViewModel.Title };

            // take the first (only) file in the Files list
            IFormFile imageFile = addImageViewModel.Files.First();

            if (imageFile.Length > 0)
            {
                using (Stream fileStream = imageFile.OpenReadStream())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        imageForCreation.Bytes = ms.ToArray();
                    }
            }

            // serialize it
            string serializedImageForCreation = JsonConvert.SerializeObject(imageForCreation);

            // call the API
            Debug.WriteLine($"QQHQ :: CALLING API");

            HttpClient httpClient = await this._imageGalleryHttpClient.GetClient();

            HttpResponseMessage response = await httpClient.PostAsync(
                    $"api/images",
                    new StringContent(serializedImageForCreation, Encoding.Unicode, "application/json"))
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return this.RedirectToAction("Index");
            }

            throw new Exception($"A problem happened while calling the API: {response.ReasonPhrase}");
        }

        public async Task Logout()
        {
            // QQHQ :: REVOKETOKENS
            HttpClient httpClient = new HttpClient();

            Debug.WriteLine($"QQHQ :: GET OIDC DISCOVERY");
            DiscoveryResponse discoveryResponse = await httpClient.GetDiscoveryDocumentAsync("https://localhost:44314");

            // Get and revoke access token
            Debug.WriteLine($"QQHQ :: EXTRACT ACCESS TOKEN");

            string accessToken = await this.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);
            if (!String.IsNullOrWhiteSpace(accessToken))
            {
                Debug.WriteLine($"QQHQ :: REVOKE ACCESS TOKEN");

                TokenRevocationResponse revocationResponse = await httpClient.RevokeTokenAsync(new TokenRevocationRequest()
                {
                    Address = discoveryResponse.RevocationEndpoint,
                    ClientId = "imagegalleryclient",
                    ClientSecret = "Client-Token-Secret",

                    Token = accessToken,
                    TokenTypeHint = "access_token", // QQHQ :: https://github.com/IdentityServer/IdentityServer4/blob/master/src/IdentityServer4/src/Constants.cs#L251
                });

                if (revocationResponse.IsError)
                {
                    throw new Exception(
                        $"Problem encountered while revoking the access token {discoveryResponse.RevocationEndpoint} :: {revocationResponse.Error}",
                        revocationResponse.Exception);
                }
            }

            // Get and revoke refresh token
            Debug.WriteLine($"QQHQ :: EXTRACT REFRESH TOKEN");

            string refreshToken = await this.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.RefreshToken);
            if (!String.IsNullOrWhiteSpace(refreshToken))
            {
                Debug.WriteLine($"QQHQ :: REVOKE REFRESH TOKEN");

                TokenRevocationResponse revocationResponse = await httpClient.RevokeTokenAsync(new TokenRevocationRequest()
                {
                    Address = discoveryResponse.RevocationEndpoint,
                    ClientId = "imagegalleryclient",
                    ClientSecret = "Client-Token-Secret",

                    Token = refreshToken,
                    TokenTypeHint = "refresh_token", // QQHQ :: https://github.com/IdentityServer/IdentityServer4/blob/master/src/IdentityServer4/src/Constants.cs#L251
                });

                if (revocationResponse.IsError)
                {
                    throw new Exception(
                        $"Problem encountered while revoking the refresh token {discoveryResponse.RevocationEndpoint} :: {revocationResponse.Error}",
                        revocationResponse.Exception);
                }
            }

            // QQHQ :: LOGOUT :: Clears the local/client-side cookies (aka. logout of the client only). Schema must match name from scheme setup.
            Debug.WriteLine($"QQHQ :: SIGNOUT COOKIES AND OIDC");

            await this.HttpContext.SignOutAsync("Cookies");
            await this.HttpContext.SignOutAsync("oidc");
        }

        // QQHQ :: ROLES :: Only user in the specific role(s) can access such action even outside of the form access point (e.g. copy link and paste)
        //[Authorize(Roles = "PayingUser")]
        // QQHQ :: ABAC
        [Authorize(Policy = "CanOrderFrame")]
        public async Task<IActionResult> OrderFrame()
        {
            await this.WriteOutIdentityInformation();

            HttpClient httpClient = new HttpClient();

            // QQHQ :: USERINFO :: Access discovery endpoint for UserInfo endpoints
            Debug.WriteLine($"QQHQ :: GET OIDC DISCOVERY");
            DiscoveryResponse discoveryResponse = await httpClient.GetDiscoveryDocumentAsync("https://localhost:44314/");

            // QQHQ :: Get access token. IDP Call???
            Debug.WriteLine($"QQHQ :: EXTRACT ACCESS TOKEN");
            string accessToken = await this.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            // QQHQ :: Manually get additional user info claim data that were not returned within identity token
            Debug.WriteLine($"QQHQ :: GET USER INFO");

            UserInfoResponse userInfoResponse = await httpClient.GetUserInfoAsync(new UserInfoRequest
            {
                Address = discoveryResponse.UserInfoEndpoint,
                Token = accessToken
            });

            if (userInfoResponse.IsError)
            {
                throw new Exception(
                    $"Problem accessing the UserInfo endpoint {discoveryResponse.UserInfoEndpoint} :: {userInfoResponse.Error}",
                    userInfoResponse.Exception);
            }

            // QQHQ :: Extract returned claim data
            string address = userInfoResponse
                .Claims
                .FirstOrDefault(claim => claim.Type == "address")?.Value;

            Debug.WriteLine($"QQHQ :: ...*RENDERING ORDER PAGE");

            return this.View(new OrderFrameViewModel(address));
        }


        private async Task WriteOutIdentityInformation()
        {
            StringBuilder str = new StringBuilder();

            /* QQHQ :: Such info should be as small as it need to be to keep the cookie light on authentication.
             If extra user info needed, it should be done through get UserInfo request flow. */
            str.AppendLine("================================ OIDC IDENTITY TOKEN INFO ======================================");

            // IDP Call???
            Debug.WriteLine($"QQHQ :: EXTRACT ID TOKEN");

            string identityToken = await this.HttpContext.GetTokenAsync(OpenIdConnectParameterNames.IdToken);

            str.AppendLine($"Identity token: {identityToken}");

            foreach (Claim claim in this.User.Claims)
            {
                str.AppendLine($"Claim type: {claim.Type} - Claim value: {claim.Value}");
            }

            str.AppendLine("================================ OIDC IDENTITY TOKEN INFO ======================================");

            Debug.WriteLine(str.ToString());
        }
    }
}