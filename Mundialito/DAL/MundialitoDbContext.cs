using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Mundialito.DAL;


public class MundialitoDbContext : IdentityDbContext<MundialitoUser>
{

	public DbSet<Game> Games { get; set; }
	public DbSet<Team> Teams { get; set; }
	public DbSet<Stadium> Stadiums { get; set; }
	public DbSet<Bet> Bets { get; set; }
	public DbSet<GeneralBet> GeneralBets { get; set; }
	public DbSet<ActionLog> ActionLogs { get; set; }
	public DbSet<Player> Players { get; set; }

	public MundialitoDbContext(DbContextOptions<MundialitoDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Game>().HasOne(e => e.HomeTeam)
				.WithMany(t => t.HomeMatches)
				.HasForeignKey(m => m.HomeTeamId)
				.OnDelete(DeleteBehavior.NoAction)
				.IsRequired();

		modelBuilder.Entity<Game>().HasOne(m => m.AwayTeam)
				.WithMany(t => t.AwayMatches)
				.HasForeignKey(m => m.AwayTeamId)
				.OnDelete(DeleteBehavior.NoAction)
				.IsRequired();

		modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => l.UserId);
		modelBuilder.Entity<IdentityRole>().HasKey(r => r.Id);
		modelBuilder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.RoleId, r.UserId });

		modelBuilder.Entity<MundialitoUser>()
						.HasIndex(u => u.Email)
						.IsUnique();
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options) =>
	options.UseSqlite("DataSource = identityDb.db; Cache=Shared");
}