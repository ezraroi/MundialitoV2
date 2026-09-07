namespace Mundialito.Logic;

/// <summary>
/// A bet write that breaks a rule of the game - a bad or missing bet, a passed deadline.
///
/// These types exist so controllers can catch what they mean instead of
/// <c>catch (Exception)</c>. A bare catch-all is what let a NullReferenceException be
/// reported to players as a validation error for months, invisible to server side Sentry
/// and to the 5xx metrics. Anything outside these types propagates to a 500 with a stack
/// trace, which is the point.
/// </summary>
public class BetValidationException : Exception
{
    public BetValidationException(string message) : base(message) { }
}

/// <summary>The game's betting deadline has passed. Its own type so "you were too late"
/// can be answered separately from "your request was malformed".</summary>
public class GameClosedForBettingException : BetValidationException
{
    public GameClosedForBettingException(string message) : base(message) { }
}

/// <summary>The bet exists but does not belong to the caller. Deliberately outside the
/// <see cref="BetValidationException"/> hierarchy so a controller cannot answer an
/// authorization failure with a validation status by catching the base type.</summary>
public class BetForbiddenException : Exception
{
    public BetForbiddenException(string message) : base(message) { }
}
