using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Mundialito.Auth.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Mundialito.Auth;
using Mundialito.Configuration;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using Mundialito.Logic;
using Mundialito.Mail;
using Mundialito.Models;

// https://medium.com/medialesson/how-to-send-emails-at-scale-in-net-with-the-azure-communication-service-14565d84147f
// https://www.telerik.com/blogs/new-net-8-aspnet-core-identity-how-implement
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseSentry();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.Configure<Config>(builder.Configuration.GetSection(Config.Key));
builder.Services.Configure<JwtTokenSettings>(builder.Configuration.GetSection(JwtTokenSettings.Key));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
builder.Services.AddControllers().AddJsonOptions(opt =>
{
	opt.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
	opt.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeJsonConverter());
	opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MundialitoDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient("MyHttpClient").ConfigurePrimaryHttpMessageHandler(() =>
			{
				var handler = new HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true // Ignore SSL certificate validation
				};
				return handler;
			});
builder.Services.AddSwaggerGen(opt =>
{
	opt.SwaggerDoc("v1", new OpenApiInfo { Title = "MundialitoV2", Version = "v1" });
	opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Description = "Please enter token",
		Name = "Authorization",
		Type = SecuritySchemeType.Http,
		BearerFormat = "JWT",
		Scheme = "bearer"
	});

	opt.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type=ReferenceType.SecurityScheme,
					Id="Bearer"
				}
			},
			new string[]{}
		}
	});
});

builder.Services.AddIdentity<MundialitoUser, IdentityRole>(
	options =>
	{
		options.Lockout.AllowedForNewUsers = true;
	    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
	    options.Lockout.MaxFailedAccessAttempts = 3;
		options.User.RequireUniqueEmail = true;
		options.SignIn.RequireConfirmedAccount = false;
		options.SignIn.RequireConfirmedEmail = false;
		options.Password.RequireDigit = false;
		options.Password.RequireLowercase = false;
		options.Password.RequireNonAlphanumeric = false;
		options.Password.RequireUppercase = false;
		options.Password.RequiredLength = 6;
	})
	.AddDefaultTokenProviders()
	.AddEntityFrameworkStores<MundialitoDbContext>();

var validIssuer = builder.Configuration.GetValue<string>("JwtTokenSettings:ValidIssuer");
var validAudience = builder.Configuration.GetValue<string>("JwtTokenSettings:ValidAudience");
var symmetricSecurityKey = builder.Configuration.GetValue<string>("JwtTokenSettings:SymmetricSecurityKey");
// The signing key is a secret and is deliberately not committed. Anyone holding it can forge
// a token for any user, so fail loudly at startup rather than booting with a null or weak key
// and issuing tokens nobody can trust. HMAC-SHA256 wants at least 256 bits.
if (string.IsNullOrWhiteSpace(symmetricSecurityKey) || Encoding.UTF8.GetByteCount(symmetricSecurityKey) < 32)
{
	throw new InvalidOperationException(
		"JwtTokenSettings:SymmetricSecurityKey is missing or shorter than 32 bytes. " +
		"Set it in configuration - on App Service as the application setting " +
		"JwtTokenSettings__SymmetricSecurityKey. Locally, scripts/dev.sh supplies a throwaway key.");
}
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
	.AddJwtBearer(options =>
	{
		options.IncludeErrorDetails = true;
		options.TokenValidationParameters = new TokenValidationParameters()
		{
			ClockSkew = TimeSpan.Zero,
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = validIssuer,
			ValidAudience = validAudience,
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(symmetricSecurityKey)
			),
		};
	});
builder.Services.AddScoped<ITeamsRepository, TeamsRepository>();
builder.Services.AddScoped<IGamesRepository, GamesRepository>();
builder.Services.AddScoped<IStadiumsRepository, StadiumsRepository>();
builder.Services.AddScoped<IPlayersRepository, PlayersRepository>();
builder.Services.AddScoped<IBetsRepository, BetsRepository>();
builder.Services.AddScoped<IGeneralBetsRepository, GeneralBetsRepository>();
builder.Services.AddScoped<IBetValidator, BetValidator>();
builder.Services.AddScoped<IBetsResolver, BetsResolver>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<IActionLogsRepository, ActionLogsRepository>();
// Singleton on purpose: it resolves a scope of its own per write, so it must not be bound
// to a request's scope - that is the whole point of it. See ActionLogger.
builder.Services.AddSingleton<IActionLogger, ActionLogger>();
builder.Services.AddScoped<TokenService, TokenService>();
builder.Services.AddScoped<GoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<AuthService, AuthService>();
builder.Services.AddScoped<TableBuilder, TableBuilder>();
builder.Services.AddScoped<TournamentTimesUtils, TournamentTimesUtils>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<GeneralBetsService, GeneralBetsService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
// Scoped, so the memoized caller lives for exactly one request - a per request lookup,
// not a cache.
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
// Authorization reads the role from the database per request rather than from the role
// claim baked into the 60 day JWT, so activating or deactivating a user takes effect at
// once instead of at their next login. Scoped, not singleton: the chain reaches the
// (scoped) MundialitoDbContext.
builder.Services.AddScoped<ICurrentUserRoleProvider, CurrentUserRoleProvider>();
builder.Services.AddScoped<IAuthorizationHandler, CurrentRoleHandler>();
builder.Services.AddScoped<IAuthorizationMiddlewareResultHandler, ForbiddenMessageResultHandler>();
builder.Services.AddAuthorization(options =>
{
	// The scheme is named explicitly so these policies do not depend on AddAuthentication
	// (above) having overridden the schemes AddIdentity registered before it.
	options.AddPolicy(Policies.ActiveOrAdmin, policy => policy
		.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
		.RequireAuthenticatedUser()
		.AddRequirements(new CurrentRoleRequirement(Role.Active, Role.Admin)));
	options.AddPolicy(Policies.AdminOnly, policy => policy
		.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
		.RequireAuthenticatedUser()
		.AddRequirements(new CurrentRoleRequirement(Role.Admin)));
});
builder.Services.AddCors(options =>
		{
			options.AddPolicy("CorsPolicy", builder =>
			{
				builder.AllowAnyOrigin()
					   .AllowAnyMethod()
					   .AllowAnyHeader();
			});
		});
builder.Services.AddProblemDetails();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
   opt.TokenLifespan = TimeSpan.FromHours(24));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
if (!builder.Environment.IsDevelopment())
{
	builder.Logging.AddApplicationInsights(
			configureTelemetryConfiguration: (config) =>
				config.ConnectionString = builder.Configuration.GetValue<string>("APPLICATIONINSIGHTS_CONNECTION_STRING"),
				configureApplicationInsightsLoggerOptions: (options) => { }
		);
}
else
{
	builder.Logging.ClearProviders();
	builder.Logging.AddConsole();
}
var app = builder.Build();
app.UseForwardedHeaders();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.Logger.LogInformation("Running in Dev mode");
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}
app.UseCors("CorsPolicy");
app.UseSwagger();
app.UseSwaggerUI();
app.MapFallbackToController("Index", "Home");
app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-store";
        }
    }
});
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
/* The SPA fallback above is a catch-all, so without this an unmatched API route answers
   with index.html and a 200 - a deleted or mistyped endpoint looks like a success to
   $http, which reads BetId off an HTML string. Catch-all segments lose to every attribute
   route, so this only ever sees requests no controller claimed. Two consequences worth
   knowing: a wrong method on a real route answers 404 rather than 405, and any future
   non-controller endpoint under api/ (MapIdentityApi and friends) must be mapped before
   this line or it will be shadowed. */
app.Map("/api/{**path}", (string path) => Results.NotFound(new ErrorMessage { Message = $"No API endpoint at 'api/{path}'" }));
app.MapControllerRoute(
	 name: "default",
	 pattern: "{controller=Home}/{action=Index}/{id?}");
app.Logger.LogInformation("Starting Database Seeding");
DatabaseInitilaizer.Seed(app);
app.Logger.LogInformation("Database Seeding Done");
app.Run();
