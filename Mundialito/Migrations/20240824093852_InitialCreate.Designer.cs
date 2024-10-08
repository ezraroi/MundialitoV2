﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Mundialito.DAL;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mundialito.Migrations
{
    [DbContext(typeof(MundialitoDbContext))]
    [Migration("20240824093852_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRole", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("RoleId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.HasKey("UserId");

                    b.ToTable("AspNetUserLogins", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.Property<string>("RoleId")
                        .HasColumnType("text");

                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.HasKey("RoleId", "UserId");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserRoles", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("text");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens", (string)null);
                });

            modelBuilder.Entity("Mundialito.DAL.Accounts.MundialitoUser", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("ProfilePicture")
                        .HasColumnType("text");

                    b.Property<int>("Role")
                        .HasColumnType("integer");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers", (string)null);

                    b.HasAnnotation("Relational:JsonPropertyName", "MundialitoUser");
                });

            modelBuilder.Entity("Mundialito.DAL.Accounts.UserFollow", b =>
                {
                    b.Property<string>("FollowerId")
                        .HasColumnType("text");

                    b.Property<string>("FolloweeId")
                        .HasColumnType("text");

                    b.HasKey("FollowerId", "FolloweeId");

                    b.HasIndex("FolloweeId");

                    b.ToTable("UserFollows");
                });

            modelBuilder.Entity("Mundialito.DAL.ActionLogs.ActionLog", b =>
                {
                    b.Property<int>("ActionLogId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ActionLogId"));

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ObjectType")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int>("Type")
                        .HasColumnType("integer");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ActionLogId");

                    b.ToTable("ActionLogs");
                });

            modelBuilder.Entity("Mundialito.DAL.Bets.Bet", b =>
                {
                    b.Property<int>("BetId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "BetId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("BetId"));

                    b.Property<int>("AwayScore")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "AwayScore");

                    b.Property<string>("CardsMark")
                        .IsRequired()
                        .HasMaxLength(1)
                        .HasColumnType("character varying(1)")
                        .HasAnnotation("Relational:JsonPropertyName", "CardsMark");

                    b.Property<bool>("CardsWin")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "CardsWin");

                    b.Property<string>("CornersMark")
                        .IsRequired()
                        .HasMaxLength(1)
                        .HasColumnType("character varying(1)")
                        .HasAnnotation("Relational:JsonPropertyName", "CornersMark");

                    b.Property<bool>("CornersWin")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "CornersWin");

                    b.Property<int>("GameId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "GameId");

                    b.Property<bool>("GameMarkWin")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "GameMarkWin");

                    b.Property<int>("HomeScore")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "HomeScore");

                    b.Property<bool>("MaxPoints")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "MaxPoints");

                    b.Property<int?>("Points")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "Points");

                    b.Property<bool>("ResultWin")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "ResultWin");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "UserId");

                    b.HasKey("BetId");

                    b.HasIndex("GameId");

                    b.HasIndex("UserId", "GameId")
                        .IsUnique();

                    b.ToTable("Bets");
                });

            modelBuilder.Entity("Mundialito.DAL.Games.Game", b =>
                {
                    b.Property<int>("GameId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "GameId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("GameId"));

                    b.Property<int?>("AwayScore")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "AwayScore");

                    b.Property<int>("AwayTeamId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "AwayTeamId");

                    b.Property<string>("CardsMark")
                        .HasMaxLength(1)
                        .HasColumnType("character varying(1)")
                        .HasAnnotation("Relational:JsonPropertyName", "CardsMark");

                    b.Property<string>("CornersMark")
                        .HasMaxLength(1)
                        .HasColumnType("character varying(1)")
                        .HasAnnotation("Relational:JsonPropertyName", "CornersMark");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp without time zone")
                        .HasAnnotation("Relational:JsonPropertyName", "Date");

                    b.Property<int?>("HomeScore")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "HomeScore");

                    b.Property<int>("HomeTeamId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "HomeTeamId");

                    b.Property<string>("IntegrationsData")
                        .HasColumnType("jsonb")
                        .HasAnnotation("Relational:JsonPropertyName", "IntegrationsData");

                    b.Property<int>("StadiumId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "StadiumId");

                    b.Property<int>("Type")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "Type");

                    b.HasKey("GameId");

                    b.HasIndex("AwayTeamId");

                    b.HasIndex("HomeTeamId");

                    b.HasIndex("StadiumId");

                    b.ToTable("Games");

                    b.HasAnnotation("Relational:JsonPropertyName", "AwayMatches");
                });

            modelBuilder.Entity("Mundialito.DAL.GeneralBets.GeneralBet", b =>
                {
                    b.Property<int>("GeneralBetId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "GeneralBetId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("GeneralBetId"));

                    b.Property<int>("GoldBootPlayerId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "GoldBootPlayerId");

                    b.Property<bool>("IsResolved")
                        .HasColumnType("boolean")
                        .HasAnnotation("Relational:JsonPropertyName", "IsResolved");

                    b.Property<int?>("PlayerPoints")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "PlayerPoints");

                    b.Property<int?>("TeamPoints")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "TeamPoints");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("WinningTeamId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "WinningTeamId");

                    b.HasKey("GeneralBetId");

                    b.HasIndex("GoldBootPlayerId");

                    b.HasIndex("UserId");

                    b.HasIndex("WinningTeamId");

                    b.ToTable("GeneralBets");
                });

            modelBuilder.Entity("Mundialito.DAL.Players.Player", b =>
                {
                    b.Property<int>("PlayerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "PlayerId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("PlayerId"));

                    b.Property<string>("IntegrationsData")
                        .HasColumnType("jsonb")
                        .HasAnnotation("Relational:JsonPropertyName", "IntegrationsData");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "Name");

                    b.HasKey("PlayerId");

                    b.ToTable("Players");

                    b.HasAnnotation("Relational:JsonPropertyName", "GoldBootPlayer");
                });

            modelBuilder.Entity("Mundialito.DAL.Stadiums.Stadium", b =>
                {
                    b.Property<int>("StadiumId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "StadiumId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("StadiumId"));

                    b.Property<int>("Capacity")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "Capacity");

                    b.Property<string>("City")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "City");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "Name");

                    b.HasKey("StadiumId");

                    b.ToTable("Stadiums");

                    b.HasAnnotation("Relational:JsonPropertyName", "Stadium");
                });

            modelBuilder.Entity("Mundialito.DAL.Teams.Team", b =>
                {
                    b.Property<int>("TeamId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "TeamId");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TeamId"));

                    b.Property<string>("Flag")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "Flag");

                    b.Property<string>("IntegrationsData")
                        .HasColumnType("jsonb")
                        .HasAnnotation("Relational:JsonPropertyName", "IntegrationsData");

                    b.Property<string>("Logo")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "Logo");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "Name");

                    b.Property<string>("ShortName")
                        .IsRequired()
                        .HasMaxLength(3)
                        .HasColumnType("character varying(3)")
                        .HasAnnotation("Relational:JsonPropertyName", "ShortName");

                    b.Property<string>("TeamPage")
                        .HasColumnType("text")
                        .HasAnnotation("Relational:JsonPropertyName", "TeamPage");

                    b.Property<int?>("TournamentTeamId")
                        .HasColumnType("integer")
                        .HasAnnotation("Relational:JsonPropertyName", "TournamentTeamId");

                    b.HasKey("TeamId");

                    b.ToTable("Teams");

                    b.HasAnnotation("Relational:JsonPropertyName", "Team");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<string>", b =>
                {
                    b.HasOne("Microsoft.AspNetCore.Identity.IdentityRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Mundialito.DAL.Accounts.UserFollow", b =>
                {
                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", "Followee")
                        .WithMany("Followers")
                        .HasForeignKey("FolloweeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", "Follower")
                        .WithMany("Followees")
                        .HasForeignKey("FollowerId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Followee");

                    b.Navigation("Follower");
                });

            modelBuilder.Entity("Mundialito.DAL.Bets.Bet", b =>
                {
                    b.HasOne("Mundialito.DAL.Games.Game", "Game")
                        .WithMany()
                        .HasForeignKey("GameId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Game");

                    b.Navigation("User");
                });

            modelBuilder.Entity("Mundialito.DAL.Games.Game", b =>
                {
                    b.HasOne("Mundialito.DAL.Teams.Team", "AwayTeam")
                        .WithMany("AwayMatches")
                        .HasForeignKey("AwayTeamId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Teams.Team", "HomeTeam")
                        .WithMany("HomeMatches")
                        .HasForeignKey("HomeTeamId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Stadiums.Stadium", "Stadium")
                        .WithMany()
                        .HasForeignKey("StadiumId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AwayTeam");

                    b.Navigation("HomeTeam");

                    b.Navigation("Stadium");
                });

            modelBuilder.Entity("Mundialito.DAL.GeneralBets.GeneralBet", b =>
                {
                    b.HasOne("Mundialito.DAL.Players.Player", "GoldBootPlayer")
                        .WithMany()
                        .HasForeignKey("GoldBootPlayerId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Accounts.MundialitoUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Mundialito.DAL.Teams.Team", "WinningTeam")
                        .WithMany()
                        .HasForeignKey("WinningTeamId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("GoldBootPlayer");

                    b.Navigation("User");

                    b.Navigation("WinningTeam");
                });

            modelBuilder.Entity("Mundialito.DAL.Accounts.MundialitoUser", b =>
                {
                    b.Navigation("Followees");

                    b.Navigation("Followers");
                });

            modelBuilder.Entity("Mundialito.DAL.Teams.Team", b =>
                {
                    b.Navigation("AwayMatches");

                    b.Navigation("HomeMatches");
                });
#pragma warning restore 612, 618
        }
    }
}
