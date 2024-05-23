using System.Text;
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
    private readonly ILogger _logger;


    public AccountController(ILogger<AccountController> logger, UserManager<MundialitoUser> userManager, MundialitoDbContext context,
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
        _logger = logger;
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
            NormalizedUserName = model.UserName,
            Email = model.Email,
            NormalizedEmail = model.Email,
            LastName = model.LastName,
            FirstName = model.FirstName,
            Role = Role.User,
        };
        _logger.LogInformation("Will store user {User} with mail {Mail}", user.UserName, user.Email);
        IdentityResult result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to create user {User}({Mail})", user.UserName, user.Email);
            if (result.Errors != null)
            {
                foreach (IdentityError error in result.Errors)
                {
                    _logger.LogError("Error: {err}({code})", error.Description, error.Code);
                    ModelState.AddModelError("", error.Description);
                }
            }
            if (ModelState.IsValid)
            {
                return BadRequest();
            }
            return BadRequest(ModelState);
        }
        _logger.LogInformation("{User} created successfully", user.UserName);
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult<AuthResponse>> Authenticate(LoginVM request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var managedUser = await _userManager.FindByNameAsync(request.Username!);
        if (managedUser == null)
        {
            _logger.LogInformation("{User} does not exists", request.Username);
            return BadRequest(new ErrorMessage { Message = "Bad credentials" });
        }
        var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password!);
        if (!isPasswordValid)
        {
            _logger.LogInformation("{User} wrong password", request.Username);
            return BadRequest(new ErrorMessage { Message = "Bad credentials" });
        }
        var userInDb = _context.Users.FirstOrDefault(u => u.UserName == request.Username);
        if (userInDb is null)
        {
            _logger.LogWarning("{User} was not found in DB", request.Username);
            return Unauthorized();
        }

        var accessToken = _tokenService.CreateToken(userInDb);
        await _context.SaveChangesAsync();
        _logger.LogWarning("{User} logged in", request.Username);
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
        _logger.LogInformation("Starting change password for {User}", user.UserName);
        IdentityResult result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            _logger.LogError("Failed to reset password for {User}", user.UserName);
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogError("Error: {err}({code})", error.Description, error.Code);
            }
            return BadRequest(ModelState);
        }
        // Upon successfully changing the password refresh sign-in cookie
        _logger.LogInformation("Password reset done for user {User}, will signin", user.UserName);
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("{User} signed-in", user.UserName);
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
            return NotFound(new ErrorMessage { Message = $"No user with the provided email {forgotPasswordModel.Email} is registered" });
        _logger.LogInformation("Generating reset password token for {user}", user.UserName);
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        _logger.LogInformation("Token generated");
        StringBuilder messageBuilder = new StringBuilder();
        messageBuilder.AppendFormat("Please follow the attached link to reset your {0} password: {1}/reset?token={2}&email={3}", _config.ApplicationName, _config.LinkAddress, token, user.Email);
        _logger.LogInformation("Sending mail");
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
            return NotFound(new ErrorMessage { Message = "No user with provided mail" });
        var token = Strings.Replace(resetPasswordModel.Token, " ", "+");
        _logger.LogInformation("Reseting password for {user}", user.UserName);
        var resetPassResult = await _userManager.ResetPasswordAsync(user, token, resetPasswordModel.Password);
        if (!resetPassResult.Succeeded)
        {
            _logger.LogError("Failed to reset password for {user}. Errors: {errors}", user.UserName, resetPassResult.Errors);
            return BadRequest(new ErrorMessage { Message = "Invalid Token" });
        }
        _logger.LogInformation("Password reset done for {user}", user.UserName);
        return Ok();
    }

}
