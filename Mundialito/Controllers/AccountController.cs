
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mundialito;
using Mundialito.Configuration;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.Logic;
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

    public AccountController(UserManager<MundialitoUser> userManager, MundialitoDbContext context,
        TokenService tokenService, ILogger<AccountController> logger, IOptions<Config> config, TournamentTimesUtils tournamentTimesUtils)
    {
        _userManager = userManager;
        _context = context;
        _tokenService = tokenService;
        _config = config.Value;
        _tournamentTimesUtils = tournamentTimesUtils;
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
            FirstName = model.FirstName
        };

        IdentityResult result = await _userManager.CreateAsync(user, model.Password);
        IActionResult errorResult = GetErrorResult(result);
        if (errorResult != null)
        {
            return errorResult;
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Authenticate(LoginVM request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var managedUser = await _userManager.FindByNameAsync(request.Username!);
        
        if (managedUser == null)
        {
            return BadRequest("Bad credentials");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(managedUser, request.Password!);
        
        if (!isPasswordValid)
        {
            return BadRequest("Bad credentials");
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
            Token = accessToken,
        });
    }
}
