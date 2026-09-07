using Mundialito.DAL.Accounts;

namespace Mundialito.Auth.Authorization;

/// <summary>
/// Reads the role of the currently authenticated caller from the database.
/// This is the seam that keeps <see cref="CurrentRoleHandler"/> testable: MundialitoDbContext
/// hard-wires Npgsql in OnConfiguring and has no DbContextOptions constructor, so it cannot
/// be pointed at an in-memory provider in a unit test.
/// </summary>
public interface ICurrentUserRoleProvider
{
    /// <summary>
    /// The caller's current role, or null if there is no authenticated caller or the user
    /// no longer exists.
    /// </summary>
    Task<Role?> GetCurrentRoleAsync();
}
