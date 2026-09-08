using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Extensions;
using Mundialito.DAL.Accounts;
using Mundialito.Models;

namespace Mundialito.Auth
{
    public static class CreateUserFromSocialLoginExtension
    {
        /// <summary>
        /// Creates user from social login
        /// </summary>
        /// <param name="userManager">the usermanager</param>
        /// <param name="model">the model</param>
        /// <returns>System.Threading.Tasks.Task&lt;User&gt;</returns>
        public static async Task<MundialitoUser?> CreateUserFromSocialLogin(this UserManager<MundialitoUser> userManager, CreateUserFromSocialLogin model, LoginProvider loginProvider, string adminEmail, ILogger logger)
        {
            //CHECKS IF THE USER HAS NOT ALREADY BEEN LINKED TO AN IDENTITY PROVIDER
            var user = await userManager.FindByLoginAsync(loginProvider.GetDisplayName(), model.LoginProviderSubject);
            if (user is not null)
                return user; //USER ALREADY EXISTS.
            user = await userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                // Last resort guard. FirstName and LastName are NOT NULL columns, and a null
                // reaching them is a 500 the signing-up user can never get past (#167).
                // GoogleAuthService already fills them in; this is here so a second provider
                // cannot reintroduce the same crash.
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.FirstName) || model.LastName is null)
                {
                    logger.LogError("Refusing to create a social login user without an email or a name");
                    return null;
                }

                // The email local part on its own is not unique - roi@gmail.com and
                // roi@outlook.com both wanted "roi" and the second one could never sign up.
                var userName = await SocialLoginIdentity.ResolveUserNameAsync(
                    model.Email,
                    userManager.Options.User.AllowedUserNameCharacters,
                    async candidate => await userManager.FindByNameAsync(candidate) is not null);
                if (userName is null)
                {
                    logger.LogError("Could not derive a free username for {Mail}", model.Email);
                    return null;
                }

                user = new MundialitoUser
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = userName,
                    Role = model.Email == adminEmail ? Role.Admin : Role.Disabled,
                    ProfilePicture = model.ProfilePicture,
                    //EMAIL IS CONFIRMED; IT IS COMING FROM AN IDENTITY PROVIDER
                    EmailConfirmed = true,
                };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    // Discarding this used to let a failed create fall through to AddLoginAsync,
                    // which then inserted a login row for a user that was never persisted. It is
                    // also the only thing that catches two simultaneous signups racing for the
                    // same username, which the probe above cannot.
                    foreach (var error in createResult.Errors)
                        logger.LogError("Failed to create user for {Mail}: {Error}({Code})", model.Email, error.Description, error.Code);
                    return null;
                }
                logger.LogInformation("Created user {User} for {Mail}", user.UserName, user.Email);
            }

            UserLoginInfo? userLoginInfo = null;
            switch (loginProvider)
            {
                case LoginProvider.Google:
                    {
                        userLoginInfo = new UserLoginInfo(loginProvider.GetDisplayName(), model.LoginProviderSubject, loginProvider.GetDisplayName().ToUpper());
                    }
                    break;

                default:
                    break;
            }
            //ADDS THE USER TO AN IDENTITY PROVIDER
            var result = await userManager.AddLoginAsync(user, userLoginInfo);
            if (result.Succeeded)
                return user;
            foreach (var error in result.Errors)
                logger.LogError("Failed to link {User} to {Provider}: {Error}({Code})", user.UserName, loginProvider, error.Description, error.Code);
            return null;
        }
    }
}
