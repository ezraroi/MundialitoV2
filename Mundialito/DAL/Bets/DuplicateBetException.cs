namespace Mundialito.DAL.Bets;

/// <summary>
/// The (user, game) pair is already taken - IX_Bets_UserId_GameId. Raised by
/// <see cref="BetsRepository.Save"/> so that callers never have to know a SQLSTATE:
/// the driver's unique_violation is a DAL concern, not an HTTP one.
/// </summary>
public class DuplicateBetException : Exception
{
    public DuplicateBetException(string message, Exception innerException)
        : base(message, innerException) { }
}
