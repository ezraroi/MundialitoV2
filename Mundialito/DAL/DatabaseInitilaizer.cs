

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL;

public class DatabaseInitilaizer
{

    private Dictionary<String, Stadium> stadiumsDic = new Dictionary<string, Stadium>();
    private Dictionary<String, Team> teamsDic = new Dictionary<string, Team>();
    private Dictionary<String, Player> playersDic = new Dictionary<string, Player>();
    private readonly UserManager<MundialitoUser> userManager;
    private bool monkeyEnabled = false;
    private readonly Config config;

    public DatabaseInitilaizer(UserManager<MundialitoUser> userManager, IOptions<Config> config)
    {
        this.userManager = userManager;
        this.config = config.Value;
    }
    
    public static void Seed(IApplicationBuilder applicationBuilder)
    {
        using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<MundialitoDbContext>();
            
        }
    }
}