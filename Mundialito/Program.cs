using Microsoft.AspNetCore.Identity;
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

// https://www.telerik.com/blogs/new-net-8-aspnet-core-identity-how-implement
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.Configure<Config>(builder.Configuration.GetSection(Config.Key));
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<MundialitoDbContext>();
builder.Services.AddEndpointsApiExplorer();
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
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<MundialitoDbContext>();
// builder.Services.AddDefaultIdentity<IdentityUser>(options =>
// {
// 	options.SignIn.RequireConfirmedAccount = false;
// 	options.SignIn.RequireConfirmedEmail = false;
// 	options.Password.RequireDigit = false;
// 	options.Password.RequireLowercase = false;
// 	options.Password.RequireNonAlphanumeric = false;
// 	options.Password.RequireUppercase = false;
// 	options.Password.RequiredLength = 6;
// }).AddRoles<IdentityRole>().AddEntityFrameworkStores<MundialitoDbContext>();

// builder.Services.AddIdentityApiEndpoints<MundialitoUser>(options =>
// {
// 	options.SignIn.RequireConfirmedAccount = false;
// 	options.SignIn.RequireConfirmedEmail = false;
// 	options.Password.RequireDigit = false;
// 	options.Password.RequireLowercase = false;
// 	options.Password.RequireNonAlphanumeric = false;
// 	options.Password.RequireUppercase = false;
// 	options.Password.RequiredLength = 6;
// }
// 	).AddRoles<IdentityRole>().AddEntityFrameworkStores<MundialitoDbContext>();


builder.Services.AddScoped<ILoggedUserProvider, LoggedUserProvider>();
builder.Services.AddScoped<ITeamsRepository, TeamsRepository>();
builder.Services.AddScoped<IGamesRepository, GamesRepository>();
builder.Services.AddScoped<IStadiumsRepository, StadiumsRepository>();
builder.Services.AddScoped<IPlayersRepository, PlayersRepository>();
builder.Services.AddScoped<IBetsRepository, BetsRepository>();
builder.Services.AddScoped<IGeneralBetsRepository, GeneralBetsRepository>();
builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<IBetValidator, BetValidator>();
builder.Services.AddScoped<IBetsResolver, BetsResolver>();
builder.Services.AddScoped<ILoggedUserProvider, LoggedUserProvider>();
builder.Services.AddScoped<IUsersRetriver, UsersRetriver>();
builder.Services.AddScoped<IDateTimeProvider, DateTimeProvider>();
builder.Services.AddScoped<IActionLogsRepository, ActionLogsRepository>();
builder.Services.AddScoped<IAdminManagment, AdminManagment>();

var app = builder.Build();

DatabaseInitilaizer.Seed(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseSwagger();
	app.UseSwaggerUI();
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();



app.MapControllerRoute(
	 name: "default",
	 pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGroup("/identity").MapIdentityApi<IdentityUser>();
// app.MapIdentityApi<IdentityUser>();

app.Run();


