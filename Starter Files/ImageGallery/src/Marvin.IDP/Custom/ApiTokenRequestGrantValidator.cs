using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Test;
using IdentityServer4.Validation;
using Marvin.IDP;

namespace IdentityProvider.Custom
{
    public static class CustomGrantType
    {
        public static string ApiTokenRequest = "api_token_request";
    }

    public class CustomGrantTypes
    {
        public static ICollection<string> ResourceOwnersAndClientCredentials =>
            new[] { CustomGrantType.ApiTokenRequest, GrantType.ResourceOwnerPassword, GrantType.ClientCredentials };

        public static ICollection<string> ApiTokenRequestAndClientCredentials =>
            new[] { CustomGrantType.ApiTokenRequest, GrantType.ClientCredentials };

        public static ICollection<string> ApiTokenRequest =>
            new[] { CustomGrantType.ApiTokenRequest };
    }

    public class ApiTokenRequestGrantValidator : IExtensionGrantValidator
    {
        private readonly ITokenValidator _validator;

        public ApiTokenRequestGrantValidator(ITokenValidator validator)
        {
            _validator = validator;
        }

        public string GrantType => CustomGrantType.ApiTokenRequest;

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            // Validate access client access token
            var userToken = context.Request.Raw.Get("accessToken");

            if (string.IsNullOrEmpty(userToken))
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
                return;
            }

            var result = await _validator.ValidateAccessTokenAsync(userToken);
            if (result.IsError)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant);
                return;
            }

            // get user's identity
            var userSubjectId = context.Request.Raw.Get("userSubjectId");

            TestUser user = Config.GetUsers().FirstOrDefault(item => item.SubjectId == userSubjectId) ??
                            Config.GetUsers()[0];

            context.Result = new GrantValidationResult(
                user.SubjectId ?? throw new ArgumentException("Subject ID not set", nameof(user.SubjectId)),
                GrantType,
                user.Claims);

            return;
        }
    }
}