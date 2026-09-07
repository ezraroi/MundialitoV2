namespace Mundialito.Auth.Authorization;

/// <summary>
/// Names of the authorization policies registered in Program.cs.
/// These replace the role strings that used to be passed to [Authorize(Roles = "...")].
/// The role in the JWT is a 60 day old snapshot; these policies read the role from the
/// database on every request instead, so activating or deactivating a user takes effect
/// immediately rather than at their next login.
/// </summary>
public static class Policies
{
    public const string ActiveOrAdmin = "ActiveOrAdmin";

    public const string AdminOnly = "AdminOnly";
}
