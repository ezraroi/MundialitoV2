using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Extensions;
using Mundialito.DAL;
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
        /// <param name="context">the context</param>
        /// <param name="model">the model</param>
        /// <returns>System.Threading.Tasks.Task&lt;User&gt;</returns>
        public static async Task<MundialitoUser?> CreateUserFromSocialLogin(this UserManager<MundialitoUser> userManager, MundialitoDbContext context, CreateUserFromSocialLogin model, LoginProvider loginProvider, string adminEmail)
        {
            //CHECKS IF THE USER HAS NOT ALREADY BEEN LINKED TO AN IDENTITY PROVIDER
            var user = await userManager.FindByLoginAsync(loginProvider.GetDisplayName(), model.LoginProviderSubject);
            if (user is not null)
                return user; //USER ALREADY EXISTS.
            user = await userManager.FindByEmailAsync(model.Email);
            if (user is null)
            {
                user = new MundialitoUser
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    UserName = model.Email.Split('@')[0],
                    Role = model.Email == adminEmail ? Role.Admin : Role.Disabled,
                    ProfilePicture = model.ProfilePicture,
                };
                await userManager.CreateAsync(user);
                //EMAIL IS CONFIRMED; IT IS COMING FROM AN IDENTITY PROVIDER
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
                await context.SaveChangesAsync();
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
            else
                return null;
        }
    }
}