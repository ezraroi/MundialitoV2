using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// https://medium.com/medialesson/how-to-send-emails-at-scale-in-net-with-the-azure-communication-service-14565d84147f
// https://www.telerik.com/blogs/new-net-8-aspnet-core-identity-how-implement
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.Configure<Config>(builder.Configuration.GetSection(Config.Key));
builder.Services.Configure<JwtTokenSettings>(builder.Configuration.GetSection(JwtTokenSettings.Key));
builder.Services.AddControllers().AddJsonOptions(opt =>
{
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
	opt.SwaggerDoc("v1", new OpenApiInfo { Title = "Mundialito", Version = "v1" });
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
builder.Services.AddScoped<TokenService, TokenService>();
builder.Services.AddScoped<TableBuilder, TableBuilder>();
builder.Services.AddScoped<TournamentTimesUtils, TournamentTimesUtils>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
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
   opt.TokenLifespan = TimeSpan.FromHours(5));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
if (!builder.Environment.IsDevelopment())
{
	builder.Logging.AddApplicationInsights(
			configureTelemetryConfiguration: (config) =>
				config.ConnectionString = builder.Configuration.GetConnectionString("APPLICATIONINSIGHTS"),
				configureApplicationInsightsLoggerOptions: (options) => { }
		);
}
else
{
	builder.Logging.ClearProviders();
	builder.Logging.AddConsole();
}
var app = builder.Build();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
	 name: "default",
	 pattern: "{controller=Home}/{action=Index}/{id?}");
app.Logger.LogInformation("Starting Database Seeding");
DatabaseInitilaizer.Seed(app);
app.Logger.LogInformation("Database Seeding Done");
app.Run();
