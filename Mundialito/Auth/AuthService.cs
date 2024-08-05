using Microsoft.AspNetCore.Identity;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.Models;

namespace Mundialito.Auth
{
    public class AuthService
    {
        private readonly GoogleAuthService _googleAuthService;
        private readonly TokenService _tokenService;

        public AuthService(GoogleAuthService googleAuthService, TokenService tokenService)
        {
            _googleAuthService = googleAuthService;
            _tokenService = tokenService;
        }

        public async Task<string> SignInWithGoogle(GoogleSigninModel model) 
        {
            var response = await _googleAuthService.GoogleSignIn(model);
            if (response is null)
                return "";
            return  _tokenService.CreateToken(response);
        }
    }
}