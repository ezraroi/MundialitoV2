using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
	public DbSet<UserFollow> UserFollows { get; set; }
	private readonly IConfiguration appConfig;
	private readonly string _connectionString;

	public MundialitoDbContext(IConfiguration config)
	{
		appConfig = config;
	}

	public MundialitoDbContext(IConfiguration config, string connectionString)
	{
		appConfig = config;
		_connectionString = connectionString;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Game>().HasOne(e => e.HomeTeam)
				.WithMany(t => t.HomeMatches)
				.HasForeignKey(m => m.HomeTeamId)
				.OnDelete(DeleteBehavior.NoAction)
				.IsRequired();

		var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
			(c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null),
			c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null).GetHashCode(),
			c => JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null), (JsonSerializerOptions)null)
		);

		modelBuilder.Entity<Game>()
			.Property(g => g.IntegrationsData)
			.HasColumnType("jsonb")
			.HasConversion(
				v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
				v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null))
			.Metadata.SetValueComparer(dictionaryComparer);

		modelBuilder.Entity<Team>()
            .Property(t => t.IntegrationsData)
			.HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null))
            .Metadata.SetValueComparer(dictionaryComparer);

		modelBuilder.Entity<Game>().HasOne(m => m.AwayTeam)
				.WithMany(t => t.AwayMatches)
				.HasForeignKey(m => m.AwayTeamId)
				.OnDelete(DeleteBehavior.NoAction)
				.IsRequired();

		modelBuilder.Entity<Bet>()
            .HasIndex(b => new { b.UserId, b.GameId })
            .IsUnique();

		modelBuilder.Entity<UserFollow>()
					.HasKey(uf => new { uf.FollowerId, uf.FolloweeId });
		modelBuilder.Entity<UserFollow>()
			.HasOne(uf => uf.Follower)
			.WithMany(u => u.Followees)
			.HasForeignKey(uf => uf.FollowerId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<UserFollow>()
			.HasOne(uf => uf.Followee)
			.WithMany(u => u.Followers)
			.HasForeignKey(uf => uf.FolloweeId)
			.OnDelete(DeleteBehavior.Restrict);

		modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => l.UserId);
		modelBuilder.Entity<IdentityRole>().HasKey(r => r.Id);
		modelBuilder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.RoleId, r.UserId });

		modelBuilder.Entity<MundialitoUser>()
						.HasIndex(u => u.Email)
						.IsUnique();
	}

	protected override void OnConfiguring(DbContextOptionsBuilder options)
	{
		AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
		AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
		options.UseNpgsql(string.IsNullOrEmpty(_connectionString) ? appConfig.GetConnectionString("App") : _connectionString);
	}
}