using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.DAL.Accounts;
using Mundialito.Models;
using Google.Apis.Auth;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace Mundialito.Auth
{
    public class GoogleAuthService
    {
        private readonly UserManager<MundialitoUser> _userManager;
        private readonly Config _config;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(UserManager<MundialitoUser> userManager, IOptions<Config> googleAuthConfig, ILogger<GoogleAuthService> logger)
        {
            _userManager = userManager;
            _config = googleAuthConfig.Value;
            _logger = logger;
        }

        public async Task<MundialitoUser?> GoogleSignIn(GoogleSigninModel model)
        {
            Payload payload;
            try
            {
                payload = await ValidateAsync(model.Credential, new ValidationSettings
                {
                    Audience = [_config.GoogleClientId]
                });
            }
            catch (InvalidJwtException e)
            {
                // An expired or malformed credential is the caller presenting bad input, not a
                // server fault. Letting this escape answered 500 where the client already knows
                // how to show the 400.
                _logger.LogWarning("Rejected a Google credential: {Message}", e.Message);
                return null;
            }

            // The only claim this application genuinely cannot do without: it keys the unique
            // index, seeds the username and decides who is the admin. Splitting a null one used
            // to throw before Identity ever saw it.
            if (string.IsNullOrWhiteSpace(payload.Email))
            {
                _logger.LogWarning("Google credential for subject {Subject} carries no email", payload.Subject);
                return null;
            }

            // given_name and family_name are optional claims, and FirstName/LastName are NOT
            // NULL - see #167. payload.Name is the display name Google sends when it has not
            // split the name into two.
            var (firstName, lastName) = SocialLoginIdentity.ResolveName(payload.GivenName, payload.FamilyName, payload.Name, payload.Email);

            var userToBeCreated = new CreateUserFromSocialLogin
            {
                FirstName = firstName,
                LastName = lastName,
                Email = payload.Email,
                ProfilePicture = payload.Picture,
                LoginProviderSubject = payload.Subject,
            };
            return await _userManager.CreateUserFromSocialLogin(userToBeCreated, LoginProvider.Google, _config.AdminEmail, _logger);
        }
    }
}
