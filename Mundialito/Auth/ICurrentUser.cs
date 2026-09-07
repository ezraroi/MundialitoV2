using Mundialito.DAL.Accounts;

namespace Mundialito.Auth;

/// <summary>
/// The caller of the current request. Every write action used to resolve this for itself with
/// <c>userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name)</c> - a
/// database round-trip per action purely to map a name to an id, repeated in eleven places,
/// and a dependency on UserManager that made those actions awkward to unit test.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The authenticated caller, or null if there is none or the user no longer exists.
    /// Callers must keep answering 401 on null: an action reached through [Authorize] whose
    /// user has since been deleted is unauthenticated, not a server fault.
    /// </summary>
    Task<MundialitoUser?> GetAsync();
}
