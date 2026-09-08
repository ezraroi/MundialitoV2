namespace Mundialito.Auth;

/// <summary>
/// Derives the fields a social login has to supply but an OIDC provider is under no obligation
/// to send.
///
/// given_name and family_name are optional claims - brand and organisation accounts, and
/// personal accounts whose name was never split, carry only a display name. FirstName and
/// LastName are character varying(100) NOT NULL, so mapping the claims straight through made
/// Postgres reject the insert with 23502 and every such account got a 500 it could not retry
/// past (#167, Sentry 64113453).
///
/// Pure and static on purpose: MundialitoDbContext hard-wires Npgsql in OnConfiguring and has
/// no DbContextOptions constructor, so nothing that touches it can be unit tested. Keeping the
/// derivation here means the part with the rules in it is testable without a database or a
/// UserManager - the same reasoning written up on ICurrentUserRoleProvider.
/// </summary>
public static class SocialLoginIdentity
{
    /// <summary>MundialitoUser.FirstName / LastName are character varying(100).</summary>
    private const int MaxNameLength = 100;

    /// <summary>Leaves room for the numeric suffix ResolveUserNameAsync may append.</summary>
    private const int MaxUserNameLength = 60;

    private const int MaxUserNameAttempts = 50;

    /// <summary>
    /// A first and last name, both non-null and inside the column length. In preference order:
    /// given/family, then the display name split on whitespace, then the email local part.
    ///
    /// An empty LastName is a legitimate answer - plenty of people have one name - and an empty
    /// string satisfies the NOT NULL column, which is the whole point.
    /// </summary>
    public static (string FirstName, string LastName) ResolveName(string? givenName, string? familyName, string? displayName, string? email)
    {
        var first = Trimmed(givenName);
        var last = Trimmed(familyName);

        if (first is null)
        {
            var full = Trimmed(displayName);
            if (full is not null)
            {
                var parts = full.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                first = parts[0];
                last ??= parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : null;
            }
        }

        first ??= LocalPart(email);
        // "?" rather than "" so a name the provider never sent is visibly absent in the table
        // rather than looking like a rendering bug.
        return (Clamp(first ?? "?"), Clamp(last ?? string.Empty));
    }

    /// <summary>
    /// A free username derived from the email local part, or null when no candidate is free.
    ///
    /// The local part alone is not unique: roi@gmail.com and roi@outlook.com both yield "roi",
    /// and the second one fails Identity's unique username check. Note this probe is
    /// best effort - two simultaneous signups can both see the same candidate free, and
    /// usernames that differ only in case normalize together. Checking IdentityResult.Succeeded
    /// at the call site is what actually closes that; this loop is what stops the common case
    /// from failing at all.
    /// </summary>
    /// <param name="email">The address the account is being created for.</param>
    /// <param name="allowedCharacters">
    /// IdentityOptions.User.AllowedUserNameCharacters. Passed in rather than duplicated here so
    /// this stays correct if Program.cs ever configures it.
    /// </param>
    /// <param name="isTaken">
    /// Must go through UserManager.FindByNameAsync: Identity's unique index covers the
    /// NORMALIZED (upper invariant) username, so a raw UserName == comparison would miss a
    /// collision that differs only in case.
    /// </param>
    public static async Task<string?> ResolveUserNameAsync(string? email, string allowedCharacters, Func<string, Task<bool>> isTaken)
    {
        var stem = Sanitize(LocalPart(email), allowedCharacters) ?? "user";
        if (stem.Length > MaxUserNameLength)
            stem = stem[..MaxUserNameLength];

        if (!await isTaken(stem))
            return stem;

        for (var suffix = 2; suffix <= MaxUserNameAttempts; suffix++)
        {
            var candidate = stem + suffix;
            if (!await isTaken(candidate))
                return candidate;
        }
        return null;
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Clamp(string value) => value.Length <= MaxNameLength ? value : value[..MaxNameLength];

    private static string? LocalPart(string? email)
    {
        var trimmed = Trimmed(email);
        if (trimmed is null)
            return null;
        var at = trimmed.IndexOf('@');
        return Trimmed(at < 0 ? trimmed : trimmed[..at]);
    }

    private static string? Sanitize(string? value, string allowedCharacters)
    {
        if (value is null)
            return null;
        var kept = new string(value.Where(allowedCharacters.Contains).ToArray());
        return kept.Length == 0 ? null : kept;
    }
}
