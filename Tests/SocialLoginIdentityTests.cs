using Microsoft.AspNetCore.Identity;
using Mundialito.Auth;

namespace Tests;

/// <summary>
/// given_name and family_name are optional claims in a Google ID token, but FirstName and
/// LastName are character varying(100) NOT NULL. Mapping the claims straight through made
/// Postgres answer 23502 and every account without a given name got a 500 it could not retry
/// past - #167, Sentry 64113453. These pin the fallbacks that replaced that mapping.
/// </summary>
[TestFixture]
public class SocialLoginIdentityTests
{
    /// <summary>What Program.cs actually runs with: it never overrides this option.</summary>
    private static readonly string AllowedUserNameCharacters = new IdentityOptions().User.AllowedUserNameCharacters;

    private static Task<string?> UserNameFor(string? email, params string[] taken)
    {
        var takenNames = new HashSet<string>(taken, StringComparer.OrdinalIgnoreCase);
        return SocialLoginIdentity.ResolveUserNameAsync(email, AllowedUserNameCharacters,
            candidate => Task.FromResult(takenNames.Contains(candidate)));
    }

    [Test]
    public void BothClaimsPresent_AreUsedAsIs()
    {
        var (first, last) = SocialLoginIdentity.ResolveName("Roi", "Ezra", "Roi Ezra", "roi@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Roi"));
            Assert.That(last, Is.EqualTo("Ezra"));
        });
    }

    [Test]
    public void NoGivenName_DisplayNameIsSplit()
    {
        // The reported shape: an organisation account carrying only `name`.
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, "Barcelona FC", "info@barca.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Barcelona"));
            Assert.That(last, Is.EqualTo("FC"));
        });
    }

    [Test]
    public void NoGivenName_DisplayNameWithThreeParts_KeepsTheRemainderTogether()
    {
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, "Maria da Silva", "maria@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Maria"));
            Assert.That(last, Is.EqualTo("da Silva"));
        });
    }

    [Test]
    public void NoGivenName_SingleWordDisplayName_LeavesLastNameEmpty()
    {
        // An empty last name is a legitimate answer - it just has to not be null.
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, "Cher", "cher@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Cher"));
            Assert.That(last, Is.Empty);
        });
    }

    [Test]
    public void NoNameClaimsAtAll_FallsBackToTheEmailLocalPart()
    {
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, null, "info@barca.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("info"));
            Assert.That(last, Is.Empty);
        });
    }

    [Test]
    public void GivenNameWithoutFamilyName_TakesTheRemainderFromTheDisplayName()
    {
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, "Roi Ezra", "roi@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("Roi"));
            Assert.That(last, Is.EqualTo("Ezra"));
        });
    }

    [Test]
    public void BlankClaimsAreTreatedAsAbsent()
    {
        var (first, last) = SocialLoginIdentity.ResolveName("   ", "   ", "  ", "roi@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("roi"));
            Assert.That(last, Is.Empty);
        });
    }

    [Test]
    public void NothingToGoOn_StillReturnsNonNullNames()
    {
        // This input cannot reach the resolver today: GoogleAuthService rejects a blank email
        // before calling it and the extension guards again. The test pins the resolver's own
        // contract, which is the whole point of those guards - do not delete the email check
        // because this passes.
        var (first, last) = SocialLoginIdentity.ResolveName(null, null, null, null);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("?"));
            Assert.That(last, Is.Empty);
        });
    }

    [Test]
    public void OverlongNames_AreClampedToTheColumnLength()
    {
        // character varying(100): too long is 22001, the same shape of 500 as 23502 was.
        var displayName = new string('a', 300) + " " + new string('b', 300);

        var (first, last) = SocialLoginIdentity.ResolveName(null, null, displayName, "long@example.com");

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(100));
            Assert.That(last, Has.Length.EqualTo(100));
        });
    }

    [Test]
    public async Task UserName_NothingTaken_IsTheEmailLocalPart()
    {
        Assert.That(await UserNameFor("roi@gmail.com"), Is.EqualTo("roi"));
    }

    [Test]
    public async Task UserName_LocalPartTaken_GetsASuffix()
    {
        // roi@gmail.com and roi@outlook.com both wanted "roi"; the second could never sign up.
        Assert.That(await UserNameFor("roi@outlook.com", "roi"), Is.EqualTo("roi2"));
    }

    [Test]
    public async Task UserName_SuffixAlsoTaken_KeepsCounting()
    {
        Assert.That(await UserNameFor("roi@yahoo.com", "roi", "roi2"), Is.EqualTo("roi3"));
    }

    [Test]
    public async Task UserName_CollisionIsCaseInsensitive()
    {
        // Identity's unique index covers the normalized (upper invariant) name.
        Assert.That(await UserNameFor("Roi@gmail.com", "ROI"), Is.EqualTo("Roi2"));
    }

    [Test]
    public async Task UserName_DoesNotStealTheMonkeyPlaceholder()
    {
        // The monkey is seeded at startup before any request, so it is always already taken.
        // DatabaseInitilaizer.EnsureMonkeyBets attaches random bets to whoever holds this name.
        Assert.That(await UserNameFor("monkey@zoo.org", "monkey"), Is.EqualTo("monkey2"));
    }

    [Test]
    public async Task UserName_DisallowedCharactersAreStripped()
    {
        // Identity's default allow list is a-zA-Z0-9-._@+; a rejected name fails validation.
        Assert.That(await UserNameFor("ro!i#$%@gmail.com"), Is.EqualTo("roi"));
    }

    [Test]
    public async Task UserName_NothingSurvivesSanitising_FallsBackToAStem()
    {
        Assert.That(await UserNameFor("שלום@example.com"), Is.EqualTo("user"));
    }

    [Test]
    public async Task UserName_NoEmail_FallsBackToAStem()
    {
        Assert.That(await UserNameFor(null), Is.EqualTo("user"));
    }

    [Test]
    public async Task UserName_EveryCandidateTaken_ReturnsNull()
    {
        var taken = new[] { "roi" }.Concat(Enumerable.Range(2, 49).Select(i => "roi" + i)).ToArray();

        Assert.That(await UserNameFor("roi@gmail.com", taken), Is.Null);
    }

    [Test]
    public async Task UserName_LeavesRoomForTheSuffix()
    {
        var localPart = new string('a', 80);

        var userName = await UserNameFor(localPart + "@example.com");

        Assert.That(userName, Has.Length.EqualTo(60));
    }
}
