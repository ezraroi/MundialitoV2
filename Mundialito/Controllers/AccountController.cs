using System.Text;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Mundialito.Configuration;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.Logic;
using Mundialito.Mail;
using Mundialito.Models;
using Mundialito.ViewModels;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly UserManager<MundialitoUser> _userManager;
    private readonly MundialitoDbContext _context;
    private readonly TokenService _tokenService;
    private readonly Config _config;
    private readonly TournamentTimesUtils _tournamentTimesUtils;
    private readonly SignInManager<MundialitoUser> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailSender _emailSender;

    public AccountController(UserManager<MundialitoUser> userManager, MundialitoDbContext context,
        TokenService tokenService, IOptions<Config> config, TournamentTimesUtils tournamentTimesUtils, SignInManager<MundialitoUser> signInManager, IHttpContextAccessor httpContextAccessor, IEmailSender emailSender)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _config = config.Value;
        _tournamentTimesUtils = tournamentTimesUtils;
        _signInManager = signInManager;
        _httpContextAccessor = httpContextAccessor;
        _emailSender = emailSender;
    }

    [AllowAnonymous]
    [HttpPost("Register")]
    public async Task<IActionResult> Register(RegisterBindingModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        if (_tournamentTimesUtils.GetGeneralBetsCloseTime() < DateTime.UtcNow)
        {
            return BadRequest("Tournament is not open for registration anymore");
        }
        if (_config.PrivateKeyProtection)
        {
            if (!PrivateKeyValidator.ValidatePrivateKey(model.PrivateKey, model.Email))
            {
                return BadRequest("Invalid private key");
            }
        }
        MundialitoUser user = new MundialitoUser
        {
            UserName = model.UserName,
            Email = model.Email,
            LastName = model.LastName,
            FirstName = model.FirstName,
            Role = Role.User,
        };

        IdentityResult result = await _userManager.CreateAsync(user, model.Password);
        IActionResult errorResult = GetErrorResult(result);
        if (errorResult != null)
        {
            return errorResult;
        }
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Authenticate(LoginVM request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var managedUser = await _userManager.FindByNameAsync(request.Username!);
        if (managedUser == null)
        {
            return BadRequest(new ErrorMessage { Message = "Bad credentials" });
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password!);
        if (!isPasswordValid)
        {
            return BadRequest(new ErrorMessage { Message = "Bad credentials" });
        }
        var userInDb = _context.Users.FirstOrDefault(u => u.UserName == request.Username);
        if (userInDb is null)
        {
            return Unauthorized();
        }

        var accessToken = _tokenService.CreateToken(userInDb);
        await _context.SaveChangesAsync();
        return Ok(new AuthResponse
        {
            Username = userInDb.UserName,
            Email = userInDb.Email,
            AccessToken = accessToken,
            FirstName = userInDb.FirstName,
            LastName = userInDb.LastName,
        });
    }


    [HttpGet("UserInfo")]
    [Authorize]
    public async Task<ActionResult<UserInfoViewModel>> GetUserInfo()
    {
        var user = await _userManager.FindByNameAsync(_httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
        {
            return Unauthorized();
        }
        return Ok(new UserInfoViewModel
        {
            UserName = user.UserName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Roles = user.Role.ToString(),
        });
    }

    [HttpPost("ChangePassword")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordBindingModel model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        var user = await _userManager.FindByNameAsync(_httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
        {
            return Unauthorized(ModelState);
        }
        IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return BadRequest(ModelState);
        }
        // Upon successfully changing the password refresh sign-in cookie
        await _signInManager.RefreshSignInAsync(user);
        return Ok();
    }

    [HttpPost("Forgot")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordModel forgotPasswordModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(forgotPasswordModel);
        var user = await _userManager.FindByEmailAsync(forgotPasswordModel.Email);
        if (user == null)
            return NotFound(new ErrorMessage { Message = "No user with the provided email is registered" });
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        StringBuilder messageBuilder = new StringBuilder();
        messageBuilder.AppendFormat("Please follow the attached link to reset your {0} password: {1}/reset?token={2}&email={3}", _config.ApplicationName, _config.LinkAddress, token, user.Email);
        _emailSender.SendEmail(user.Email, string.Format("{0} Reset Password", _config.ApplicationName), messageBuilder.ToString());
        return Ok();
    }

    [HttpPost("Reset")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordModel resetPasswordModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(resetPasswordModel);
        var user = await _userManager.FindByEmailAsync(resetPasswordModel.Email);
        if (user == null)
            NotFound(new ErrorMessage { Message = "No user with provided mail" });
        var token = Strings.Replace(resetPasswordModel.Token, " ", "+");
        var resetPassResult = await _userManager.ResetPasswordAsync(user, token, resetPasswordModel.Password);
        if (!resetPassResult.Succeeded)
        {
            return BadRequest(new ErrorMessage { Message = "Invalid Token" });
        }
        return Ok();
    }

    private IActionResult GetErrorResult(IdentityResult result)
    {
        if (result == null)
        {
            throw new Exception("IdentityResult is null");
        }

        if (!result.Succeeded)
        {
            if (result.Errors != null)
            {
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            if (ModelState.IsValid)
            {
                // No ModelState errors are available to send, so just return an empty BadRequest.
                return BadRequest();
            }
            return BadRequest(ModelState);
        }
        return null;
    }
}
