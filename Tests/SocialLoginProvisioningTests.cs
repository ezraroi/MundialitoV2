using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Mundialito.Auth;
using Mundialito.DAL.Accounts;
using Mundialito.Models;

namespace Tests;

/// <summary>
/// CreateUserFromSocialLogin is the whole of the Google sign-in write path. Two of its three
/// branches are taken by every player already on the live tournament - found by provider login,
/// or found by email - and only the third creates anything. #167 changed that third branch, so
/// what these mostly guard is that the other two still do nothing.
///
/// The extension used to take a MundialitoDbContext, which hard-wires Npgsql in OnConfiguring
/// and cannot be pointed anywhere else; dropping that parameter is what made this testable.
/// UserManager's constructor null guards only its store (so the null! arguments below are safe)
/// and every method the extension calls is virtual, so the store is never reached - the same
/// technique as FakeUserManager in BetsTestHarness.
/// </summary>
[TestFixture]
public class SocialLoginProvisioningTests
{
    private const string AdminEmail = "admin@example.com";

    private sealed class ThrowingUserStore : IUserStore<MundialitoUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<string?> GetUserNameAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task SetUserNameAsync(MundialitoUser user, string? userName, CancellationToken t) => throw new NotImplementedException();
        public Task<string?> GetNormalizedUserNameAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task SetNormalizedUserNameAsync(MundialitoUser user, string? normalizedName, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> CreateAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> UpdateAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> DeleteAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<MundialitoUser?> FindByIdAsync(string userId, CancellationToken t) => throw new NotImplementedException();
        public Task<MundialitoUser?> FindByNameAsync(string normalizedUserName, CancellationToken t) => throw new NotImplementedException();
    }

    private sealed class FakeUserManager : UserManager<MundialitoUser>
    {
        private readonly MundialitoUser? byLogin;
        private readonly MundialitoUser? byEmail;
        private readonly HashSet<string> takenUserNames;
        private readonly IdentityResult createResult;

        public FakeUserManager(
            MundialitoUser? byLogin = null,
            MundialitoUser? byEmail = null,
            IEnumerable<string>? takenUserNames = null,
            IdentityResult? createResult = null)
            : base(new ThrowingUserStore(), null!, null!, null!, null!, null!, null!, null!, null!)
        {
            this.byLogin = byLogin;
            this.byEmail = byEmail;
            // Case insensitive: Identity's unique index covers the normalized user name.
            this.takenUserNames = new HashSet<string>(takenUserNames ?? [], StringComparer.OrdinalIgnoreCase);
            this.createResult = createResult ?? IdentityResult.Success;
        }

        public List<MundialitoUser> Created { get; } = [];
        public int AddLoginCalls { get; private set; }
        public UserLoginInfo? LastLogin { get; private set; }

        public override Task<MundialitoUser?> FindByLoginAsync(string loginProvider, string providerKey) => Task.FromResult(byLogin);

        public override Task<MundialitoUser?> FindByEmailAsync(string email) => Task.FromResult(byEmail);

        public override Task<MundialitoUser?> FindByNameAsync(string userName) =>
            Task.FromResult(takenUserNames.Contains(userName) ? new MundialitoUser { UserName = userName } : null);

        public override Task<IdentityResult> CreateAsync(MundialitoUser user)
        {
            if (createResult.Succeeded)
                Created.Add(user);
            return Task.FromResult(createResult);
        }

        public override Task<IdentityResult> AddLoginAsync(MundialitoUser user, UserLoginInfo login)
        {
            AddLoginCalls++;
            LastLogin = login;
            return Task.FromResult(IdentityResult.Success);
        }
    }

    private static CreateUserFromSocialLogin Payload(
        string email = "someone@example.com",
        string firstName = "Some",
        string lastName = "One",
        string subject = "google-subject-1") => new()
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            ProfilePicture = "https://example.com/photo.jpg",
            LoginProviderSubject = subject,
        };

    private static Task<MundialitoUser?> Provision(FakeUserManager userManager, CreateUserFromSocialLogin model) =>
        userManager.CreateUserFromSocialLogin(model, LoginProvider.Google, AdminEmail, NullLogger.Instance);

    private static MundialitoUser Existing(string userName, string email) => new()
    {
        Id = "existing-id",
        UserName = userName,
        Email = email,
        FirstName = "Existing",
        LastName = "Player",
        Role = Role.Active,
    };

    [Test]
    public async Task ExistingLinkedUser_IsReturnedUnchanged_AndNeverRecreated()
    {
        // The branch every player already on the live tournament takes on every sign-in.
        var existing = Existing("roez", "roi@example.com");
        var userManager = new FakeUserManager(byLogin: existing);

        var result = await Provision(userManager, Payload(email: "roi@example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(existing));
            Assert.That(existing.UserName, Is.EqualTo("roez"));
            Assert.That(existing.Role, Is.EqualTo(Role.Active));
            Assert.That(userManager.Created, Is.Empty);
            Assert.That(userManager.AddLoginCalls, Is.Zero);
        });
    }

    [Test]
    public async Task UserFoundByEmail_IsNotRecreated_AndGetsTheLogin()
    {
        // A player who registered with a password and is signing in with Google for the first
        // time: link the provider to the row they already own, never make a second one.
        var existing = Existing("roez", "roi@example.com");
        var userManager = new FakeUserManager(byEmail: existing);

        var result = await Provision(userManager, Payload(email: "roi@example.com"));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(existing));
            Assert.That(existing.UserName, Is.EqualTo("roez"));
            Assert.That(userManager.Created, Is.Empty);
            Assert.That(userManager.AddLoginCalls, Is.EqualTo(1));
            Assert.That(userManager.LastLogin!.LoginProvider, Is.EqualTo("Google"));
            Assert.That(userManager.LastLogin.ProviderKey, Is.EqualTo("google-subject-1"));
        });
    }

    [Test]
    public async Task NewUser_IsCreatedDisabled_WithTheEmailConfirmed()
    {
        var userManager = new FakeUserManager();

        var result = await Provision(userManager, Payload(email: "newcomer@example.com"));

        Assert.That(userManager.Created, Has.Count.EqualTo(1));
        var created = userManager.Created[0];
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(created));
            Assert.That(created.UserName, Is.EqualTo("newcomer"));
            Assert.That(created.Role, Is.EqualTo(Role.Disabled));
            // Set before the insert now, rather than by a second UpdateAsync round trip.
            Assert.That(created.EmailConfirmed, Is.True);
            Assert.That(userManager.AddLoginCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task NewUser_WithNamesDerivedFromADisplayName_IsCreated()
    {
        // #167: GoogleAuthService resolves these before we get here, so what this pins is that
        // a non-null name is all the extension needs to get the row in.
        var userManager = new FakeUserManager();

        var result = await Provision(userManager, Payload(email: "info@barca.com", firstName: "Barcelona", lastName: ""));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(userManager.Created[0].FirstName, Is.EqualTo("Barcelona"));
            Assert.That(userManager.Created[0].LastName, Is.Empty);
        });
    }

    [Test]
    public async Task NewUser_UserNameCollision_GetsASuffix()
    {
        // roi@gmail.com already holds "roi"; roi@outlook.com used to fail forever.
        var userManager = new FakeUserManager(takenUserNames: ["roi"]);

        var result = await Provision(userManager, Payload(email: "roi@outlook.com"));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(userManager.Created[0].UserName, Is.EqualTo("roi2"));
        });
    }

    [Test]
    public async Task NewUser_CreateFails_ReturnsNull_AndNeverAddsLogin()
    {
        // Discarding this result let a failed create fall through to AddLoginAsync, which then
        // inserted a login row for a user that was never persisted.
        var failure = IdentityResult.Failed(new IdentityError { Code = "DuplicateUserName", Description = "Taken" });
        var userManager = new FakeUserManager(createResult: failure);

        var result = await Provision(userManager, Payload());

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(userManager.AddLoginCalls, Is.Zero);
        });
    }

    [Test]
    public async Task NewUser_BlankFirstName_ReturnsNull_AndNeverCreates()
    {
        // The last resort guard: FirstName is NOT NULL, and a null reaching it was the 500.
        var userManager = new FakeUserManager();

        var result = await Provision(userManager, Payload(firstName: "  "));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(userManager.Created, Is.Empty);
            Assert.That(userManager.AddLoginCalls, Is.Zero);
        });
    }

    [Test]
    public async Task NewUser_NoEmail_ReturnsNull_AndNeverCreates()
    {
        var userManager = new FakeUserManager();

        var result = await Provision(userManager, Payload(email: ""));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(userManager.Created, Is.Empty);
        });
    }

    [Test]
    public async Task NewUser_WithTheAdminEmail_GetsTheAdminRole()
    {
        var userManager = new FakeUserManager();

        var result = await Provision(userManager, Payload(email: AdminEmail));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(userManager.Created[0].Role, Is.EqualTo(Role.Admin));
        });
    }
}
