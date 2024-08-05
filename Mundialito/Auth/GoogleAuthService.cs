using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.Models;
using static Google.Apis.Auth.GoogleJsonWebSignature;

namespace Mundialito.Auth
{
    public class GoogleAuthService
    {
        private readonly UserManager<MundialitoUser> _userManager;
        private readonly MundialitoDbContext _context;
        private readonly Config _config;

        public GoogleAuthService(UserManager<MundialitoUser> userManager, MundialitoDbContext context, IOptions<Config> googleAuthConfig)
        {
            _userManager = userManager;
            _context = context;
            _config = googleAuthConfig.Value;
        }

        public async Task<MundialitoUser?> GoogleSignIn(GoogleSigninModel model)
        {
            var payload = await ValidateAsync(model.Credential, new ValidationSettings
            {
                Audience = [_config.GoogleClientId]
            });
            var userToBeCreated = new CreateUserFromSocialLogin
            {
                FirstName = payload.GivenName,
                LastName = payload.FamilyName,
                Email = payload.Email,
                ProfilePicture = payload.Picture,
                LoginProviderSubject = payload.Subject,
            };
            return await _userManager.CreateUserFromSocialLogin(_context, userToBeCreated, LoginProvider.Google, _config.AdminEmail);
        }
    }
}