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

		// modelBuilder.Entity<MundialitoUser>()
		// 		.Property(e => e.FirstName).IsRequired(false)
		// 		.HasMaxLength(250);

		// modelBuilder.Entity<MundialitoUser>()
		// 	.Property(e => e.LastName).IsRequired(false)
		// 	.HasMaxLength(250);

		modelBuilder.Entity<Team>().HasData(new
		{
			TeamId = 1,
			Name = "Bulgaria",
			Flag = "https://upload.wikimedia.org/wikipedia/commons/9/9a/Flag_of_Bulgaria.svg",
			Logo = "https://upload.wikimedia.org/wikipedia/commons/9/9a/Flag_of_Bulgaria.svg",
			ShortName = "BUL"
		});
		modelBuilder.Entity<Team>().HasData(new
		{
			TeamId = 2,
			Name = "Germany",
			Flag = "https://upload.wikimedia.org/wikipedia/commons/b/ba/Flag_of_Germany.svg",
			Logo = "https://upload.wikimedia.org/wikipedia/commons/b/ba/Flag_of_Germany.svg",
			ShortName = "GER"
		});

	}

	protected override void OnConfiguring(DbContextOptionsBuilder options) =>
	options.UseSqlite("DataSource = identityDb.db; Cache=Shared");
}