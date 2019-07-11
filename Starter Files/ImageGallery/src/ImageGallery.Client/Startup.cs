using System.IdentityModel.Tokens.Jwt;
using IdentityModel;
using ImageGallery.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace ImageGallery.Client
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;

            /* QQHQ :: CLAIMS :: To just have claim types exactly as defined at the IS
             so we don't have lengthy http mapping name.
             To get this:
                ================================ OIDC IDENTITY TOKEN INFO ======================================
                Identity token: eyJhbGciOiJSUzI1NiIsImtpZCI6IjAzOGRiNTljYmM4ZDAxOWI1MjQ1YzNhODIzYWM5MTg3IiwidHlwIjoiSldUIn0.eyJuYmYiOjE1NTkwNjE0NTUsImV4cCI6MTU1OTA2MTc1NSwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMTQiLCJhdWQiOiJpbWFnZWdhbGxlcnljbGllbnQiLCJub25jZSI6IjYzNjk0NjU4MjQ4MzkzMTkyNS5OakU1TldNd01UQXROREZrTXkwMFptSXpMVGczTW1ZdE1XWTFOMlpsT1dVMFlXTTFNV1EzWmpZd09HSXRNbVUxTlMwME9XSTRMVGc0WldFdE5EVXlZVEl5WkRFNFlUTmgiLCJpYXQiOjE1NTkwNjE0NTUsImF0X2hhc2giOiJVcGNmdzNoQkEwOHVaNlNuUmFZeDR3Iiwic2lkIjoiNmY2Nzc1N2IwNDZhNDdkMWMyOWY2OWMzMDA1NzMyOTIiLCJzdWIiOiJGMTY0NkFBRS0zRTEwLTQxNDAtODczRC1DOUNEQTE4MUZENkIiLCJhdXRoX3RpbWUiOjE1NTkwNjE0NTIsImlkcCI6ImxvY2FsIiwiYW1yIjpbInB3ZCJdfQ.GUm1JW8pyQpHCYuBxq7TXOlmgy7FWJIdwVREW5n6mBbPqoIiN2AGwqTb_9IAP6zgFyisF8mepHr0E44_eMBm9DvwOc0yeGddVriHJlk8MQ_21gDRyM2SZVEzJcCmV7n1vq05sds4yk8koGzvtbHbJQ_0KL_hhmD4LMOEoGUVaXURoKov9dHPUDGIpASvUHhjQDDMjponP5DMcj034y9g5ZNhmNV1jPljaMFubSgoexXFwBenX3WJVRLP5gD_iCrghGy7l_5-pwLgz3HDoiTUfClWWX3fJYxtwIIAZTfIe5z9Tsb6ECLWrlk1CliTzJBYxLB40PsJFtPdICcx792Y-g
                Claim type: sub - Claim value: F1646AAE-3E10-4140-873D-C9CDA181FD6B
                Claim type: amr - Claim value: pwd
                Claim type: given_name - Claim value: Claire
                Claim type: family_name - Claim value: Underwood
                Claim type: role - Claim value: PayingUser
                ================================ OIDC IDENTITY TOKEN INFO ======================================
            Instead of:
                ================================ OIDC INFO ======================================
                Identity token: eyJhbGciOiJSUzI1NiIsImtpZCI6IjAzOGRiNTljYmM4ZDAxOWI1MjQ1YzNhODIzYWM5MTg3IiwidHlwIjoiSldUIn0.eyJuYmYiOjE1NTkwNDYwMjgsImV4cCI6MTU1OTA0NjMyOCwiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzMTQiLCJhdWQiOiJpbWFnZWdhbGxlcnljbGllbnQiLCJub25jZSI6IjYzNjk0NjQyODEyNDM4MjU2NS5NemcyWldJeE5XUXRNVGt6WVMwMFlqUTJMV0UwTlRjdE16UmlZamc1WWpsa05qQTFZak0xT1RNME0yUXRPRFl6WlMwME9UQmxMVGcxTXpFdE16VTFObVUyWVRjNVptVTQiLCJpYXQiOjE1NTkwNDYwMjgsImF0X2hhc2giOiI1RzVVUld4WlZwWWp5dldONTJWaVNBIiwic2lkIjoiN2JhZjViM2JjNDlhNzI1MjM0NjhhZjhjZGUzOTFhYTAiLCJzdWIiOiI3ODQzOTlGNS04NzFDLTQ2RjEtQkM5Ri1DNzExQUZCNTE3NDgiLCJhdXRoX3RpbWUiOjE1NTkwNDYwMTgsImlkcCI6ImxvY2FsIiwiYW1yIjpbInB3ZCJdfQ.Fl2yM-xcA8-JjuZK8Md2f0C4H0AnFtOkwUlgskTKG9hPgIZVFBcjdEK1ZvRA-7uzpGPJCSqPHv8MR-1Qpu9WKZr8m4aS8a-2awlfW79k4R26QsjGeYrhT2_dAZOgkDsG91Dqad8qDlOYbM1SJ2tk_DqHyK3GHtdxYDz5mCUvcGtLnw2th1fKh2c9cG0w_DrbsSMGuJyuoFMMWLqsZ5YnIznCM7C69sfrVXNuYhXMFLX4gKbpZ62NgAPvJ6VHNZpUCjkIIBSuHlmlpfXsinxqVAjmHWII017DG3iXcsf5T6LzoDwqsQ2FfzQEII2XrTD4VSSE-VTWE7o9rUYKF5HYUw
                Claim type: http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier - Claim value: 784399F5-871C-46F1-BC9F-C711AFB51748
                Claim type: http://schemas.microsoft.com/identity/claims/identityprovider - Claim value: local
                Claim type: http://schemas.microsoft.com/claims/authnmethodsreferences - Claim value: pwd
                Claim type: given_name - Claim value: Frank
                Claim type: family_name - Claim value: Underwood
                ....
                ================================ OIDC INFO ======================================
            */
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // register an IHttpContextAccessor so we can access the current
            // HttpContext in services by injecting it
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // register an IImageGalleryHttpClient
            services.AddScoped<IImageGalleryHttpClient, ImageGalleryHttpClient>();

            // Add framework services.
            services.AddMvc();

            // QQHQ :: ABAC :: Setup authorization policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "CanOrderFrame",
                    policyBuilder =>
                    {
                        policyBuilder.RequireAuthenticatedUser();
                        policyBuilder.RequireClaim("country", "be", "fr", "nl");
                        policyBuilder.RequireClaim("subscriptionlevel", "PayingUser");

                        // QQHQ :: Could require Roles as part of the policy
                        //policyBuilder.RequireRole(...);
                    });
            });

            // QQHQ :: OIDC CLIENT :: Add OIDC authentication
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "Cookies";
                    options.DefaultChallengeScheme = "oidc"; // QQHQ :: Match scheme of the OIDC authentication
                })
                // QQHQ :: For storing encrypted cookies after authentication completed
                .AddCookie("Cookies", (options) =>
                {
                    options.AccessDeniedPath = "/Authorization/AccessDenied";   // QQHQ :: ROLES :: Direct to a customer page base on OIDC data stored in the cookie
                })
                .AddOpenIdConnect("oidc", options =>    // QQHQ :: OIDC authentication workflow handler
                {
                    options.ClientId = "imagegalleryclient";    // QQHQ :: Must be known by IS
                    options.Authority = "https://localhost:44314/"; // QQHQ :: IS address

                    options.SignInScheme = "Cookies"; // QQHQ ::  Match default scheme for storing successful result in cookies
                    options.ResponseType = "code id_token"; // QQHQ ::  Hybrid

                    // QQHQ ::  Let IS control through its own list for now
                    //options.CallbackPath = new PathString("...");
                    //options.SignedOutCallbackPath = new PathString("...");

                    // QQHQ :: CLAIMS :: Specifying scopes cares by this client. Not necessary but just to be clear
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");

                    // QQHQ :: Ask for additional claim
                    options.Scope.Add("address");
                    options.Scope.Add("roles"); // QQHQ :: Custom scope for custom claim

                    // QQHQ :: ABAC
                    options.Scope.Add("country");
                    options.Scope.Add("subscriptionlevel");

                    // QQHQ :: API :: API access resource. Otherwise will get API unauthorized
                    options.Scope.Add("imagegalleryapi");

                    // QQHQ :: TOKENREFRESH :: Request for offline access code
                    options.Scope.Add("offline_access");

                    options.SaveTokens = true; // QQHQ ::  Allow OIDC middleware to save the tokens return from IS
                    options.ClientSecret = "Client-Token-Secret"; // QQHQ :: Same as secret set on the server side

                    options.GetClaimsFromUserInfoEndpoint = true;   // QQHQ :: Include sub-sequence request to get claims from UserInfo endpoint

                    // QQHQ :: Ensure claim is in the claim collection (WEIRD!!!!)
                    options.ClaimActions.Remove("amr");

                    /* QQHQ :: Remove claims that are mapped for returning as part of the claim collection within the
                     identity token by default in the base OIDC middleware options . This to explicitly keep identity token
                     cookie small
                     https://github.com/aspnet/Security/blob/master/src/Microsoft.AspNetCore.Authentication.OpenIdConnect/OpenIdConnectOptions.cs#L63
                     */
                    options.ClaimActions.DeleteClaim("sid");
                    options.ClaimActions.DeleteClaim("idp");

                    /* QQHQ :: Don't need to explicitly remove since base options does not have it mapped by default.
                     If the claim data is needed, an manual UserInfo request should be done. */
                    //options.ClaimActions.DeleteClaim("address");

                    // QQHQ :: For mapping custom identity claim type to the claim Json key in the identity token to return the claim as part of the identity token
                    options.ClaimActions.MapUniqueJsonKey("role", "role");
                    // QQHQ :: API
                    options.ClaimActions.MapUniqueJsonKey("country", "country");
                    options.ClaimActions.MapUniqueJsonKey("subscriptionlevel", "subscriptionlevel");

                    // QQHQ :: ROLES :: Defines how validation of a token should happen. This allows role to be part of user for checking in the front end
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        NameClaimType = JwtClaimTypes.GivenName,
                        RoleClaimType = JwtClaimTypes.Role
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Shared/Error");
            }

            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Gallery}/{action=Index}/{id?}");
            });
        }
    }
}