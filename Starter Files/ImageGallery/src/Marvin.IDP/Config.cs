using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Test;

namespace Marvin.IDP
{
    public static class Config
    {
        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <returns></returns>
        public static List<TestUser> GetUsers()
        {
            return new List<TestUser>()
            {
                new TestUser()
                {
                    SubjectId = "PS\\quang.quach", // QQHQ :: CLIENT :: Must be unique
                    Username = "Quang",
                    Password = "password",

                    /* QQHQ :: IdentityServer4.ResponseHandling.UserInfoResponseGenerator:Information:
                     Profile service returned the following claim types: given_name family_name address role */
                    Claims = new List<Claim>()
                    {
                        new Claim("given_name", "Quang"),
                        new Claim("family_name", "Quach"),

                        // QQHQ :: CLAIMS :: Additional claim type and its value
                        new Claim("address", "Main Road X"),
                        new Claim("role", "PayingUser"),

                        // QQHQ :: ABAC
                        new Claim("country", "us"),
                        new Claim("subscriptionlevel", "PayingUser")
                    }
                },
                new TestUser()
                {
                    SubjectId = "d860efca-22d9-47fd-8249-791ba61b07c7", // QQHQ :: CLIENT :: Must be unique
                    Username = "Frank",
                    Password = "password",

                    /* QQHQ :: IdentityServer4.ResponseHandling.UserInfoResponseGenerator:Information:
                     Profile service returned the following claim types: given_name family_name address role */
                    Claims = new List<Claim>()
                    {
                        new Claim("given_name", "Frank"),
                        new Claim("family_name", "Underwood"),

                        // QQHQ :: CLAIMS :: Additional claim type and its value
                        new Claim("address", "Main Road 1"),
                        new Claim("role", "FreeUser"),

                        // QQHQ :: ABAC
                        new Claim("country", "nl"),
                        new Claim("subscriptionlevel", "FreeUser")
                    }
                },
                new TestUser()
                {
                    SubjectId = "b7539694-97e7-4dfe-84da-b4256e1ff5c7",
                    Username = "Claire",
                    Password = "password",

                    Claims = new List<Claim>()
                    {
                        new Claim("given_name", "Claire"),
                        new Claim("family_name", "Underwood"),

                        new Claim("address", "Big Street 2"),
                        new Claim("role", "PayingUser"),

                        new Claim("country", "be"),
                        new Claim("subscriptionlevel", "PayingUser")
                    }
                }
            };
        }

        /// <summary>
        /// Gets the identity resources.
        /// Mapping between scope and claims that can be returned for a given client
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            return new List<IdentityResource>()
            {
                new IdentityResources.OpenId(), // QQHQ :: SCOPES :: Required
                new IdentityResources.Profile(), // QQHQ :: Profile-Related Claims

                new IdentityResources.Address(), // QQHQ :: CLAIMS :: Additional claims
                new IdentityResource(
                    "roles", // QQHQ :: Scope name
                    "Your role(s)", // Desc
                    new List<string>() { "role" } // Claim type to return
                ),

                // QQHQ :: ABAC
                new IdentityResource(
                    "country",
                    "The country you are living in",
                    new List<string>() { "country" }),
                new IdentityResource(
                    "subscriptionlevel",
                    "Your subscription level",
                    new List<string>() { "subscriptionlevel" }),
            };
        }

        /// <summary>
        /// Gets the API resources.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>()
            {
                // QQHQ :: API :: Finance Web API
                new ApiResource(
                        "FinanceWebAPI",
                        "Finance Web API",
                        new List<string>() { "role" }) // Claim types
                    {
                        // QQHQ :: REFTOKEN
                        ApiSecrets = { new Secret("FinanceWebAPI-Token-Secret".Sha256()) }
                    },

                // QQHQ :: API
                new ApiResource(
                        "imagegalleryapi",
                        "Image Gallery API",
                        new List<string>() { "role" }) // Claim types
                    {
                        // QQHQ :: REFTOKEN
                        ApiSecrets = { new Secret("API-Token-Secret".Sha256()) }
                    },
            };
        }

        /// <summary>
        /// Gets the clients.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>()
            {
                // QQHQ :: Finance Web API Service
                new Client()
                {
                    ClientName = "FE Web API Caller",
                    ClientId = "FEWebAPI",

                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    AllowedScopes =
                    {
                        // QQHQ :: SCOPES :: Subset of what supported scopes specified in the IS through identity resource list
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,

                        // QQHQ :: CLAIMS
                        IdentityServerConstants.StandardScopes.Address, // QQHQ :: Mapping to additional claim
                        "roles", // QQHQ :: Scope name for mapping into a customer identity resource for customer claim

                        // QQHQ :: API :: Names of API that this client has access to
                        "imagegalleryapi",
                        "FinanceWebAPI",

                        // QQHQ :: ABAC
                        "country",
                        "subscriptionlevel",
                    },

                    ClientSecrets =
                    {
                        new Secret("FEWebAPI-Token-Secret".Sha256())
                    }

                },

                // QQHQ :: GW
                new Client()
                {
                    ClientName = "FE Gateway", // QQHQ :: CONSENT :: Display on the Consent page
                    ClientId = "FEGateway",
                    AllowedGrantTypes = GrantTypes.HybridAndClientCredentials, // QQHQ :: Server-side web app

                    // QQHQ :: REFTOKEN :: Token will be just an identifier (e.g. 5f3a13dcbc2b1b21359a53a92889ab5f3fc614a9bb5337b35df2bdc833ff3bca)
                    // Extra trip to the Identity Server will be done for the validation
                    AccessTokenType = AccessTokenType.Reference,

                    // QQHQ :: EXPI
                    //IdentityTokenLifetime = ....,  // Default 300 sec/5 min as is
                    //AuthorizationCodeLifetime = ...,   // Default 300 sec/5 min as is
                    AccessTokenLifetime = 360, // After this expired, API won't accept the access token anymore (in sec)

                    // QQHQ :: TOKENREFRESH
                    AllowOfflineAccess = true,
                    //AbsoluteRefreshTokenLifetime = ...  // Default 30 days
                    //RefreshTokenExpiration = ... // Default Absolute
                    UpdateAccessTokenClaimsOnRefresh = true,

                    RedirectUris = new List<string>()
                    {
                        "https://localhost:44311/gateway/signin-oidc" // QQHQ :: Return URIs of the client to receive tokens
                    },

                    // QQHQ :: LOGOUT :: To prevent confirmation prompt and ending with Logout final page
                    PostLogoutRedirectUris = new List<string>()
                    {
                        "https://localhost:44311/gateway/signout-callback-oidc"
                    },

                    AllowedScopes =
                    {
                        // QQHQ :: SCOPES :: Subset of what supported scopes specified in the IS through identity resource list
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,

                        // QQHQ :: CLAIMS
                        IdentityServerConstants.StandardScopes.Address, // QQHQ :: Mapping to additional claim
                        "roles", // QQHQ :: Scope name for mapping into a customer identity resource for customer claim

                        // QQHQ :: API :: Names of API that this client has access to
                        "imagegalleryapi",
                        "FinanceWebAPI",

                        // QQHQ :: ABAC
                        "country",
                        "subscriptionlevel",
                    },

                    ClientSecrets =
                    {
                        new Secret("FEGateway-Token-Secret".Sha256())
                    }
                },

                // QQHQ :: CLIENT
                new Client()
                {
                    ClientName = "Image Gallery", // QQHQ :: CONSENT :: Display on the Consent page
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Hybrid, // QQHQ :: Server-side web app

                    // QQHQ :: REFTOKEN :: Token will be just an identifier (e.g. 5f3a13dcbc2b1b21359a53a92889ab5f3fc614a9bb5337b35df2bdc833ff3bca)
                    AccessTokenType = AccessTokenType.Reference,

                    // QQHQ :: EXPI
                    //IdentityTokenLifetime = ....,  // Default 5 min as is
                    //AuthorizationCodeLifetime = ...,   // Default 5 min as is
                    AccessTokenLifetime = 120, // After this expired, API won't accept the access token anymore

                    // QQHQ :: TOKENREFRESH
                    AllowOfflineAccess = true,
                    //AbsoluteRefreshTokenLifetime = ...  // Default 30 days
                    //RefreshTokenExpiration = ... // Default Absolute
                    UpdateAccessTokenClaimsOnRefresh = true,

                    RedirectUris = new List<string>()
                    {
                        "https://localhost:44393/signin-oidc" // QQHQ :: Return URIs of the client to receive tokens
                    },

                    PostLogoutRedirectUris = new List<string>()
                    {
                        "https://localhost:44393/signout-callback-oidc"
                    },

                    AllowedScopes =
                    {
                        // QQHQ :: SCOPES :: Subset of what supported scopes specified in the IS through identity resource list
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,

                        // QQHQ :: CLAIMS
                        IdentityServerConstants.StandardScopes.Address, // QQHQ :: Mapping to additional claim
                        "roles", // QQHQ :: Scope name for mapping into a customer identity resource for customer claim

                        // QQHQ :: API :: Matching with API resource name
                        "imagegalleryapi",

                        // QQHQ :: ABAC
                        "country",
                        "subscriptionlevel",
                    },

                    ClientSecrets =
                    {
                        new Secret("Client-Token-Secret".Sha256())
                    }
                }
            };
        }
    }
}