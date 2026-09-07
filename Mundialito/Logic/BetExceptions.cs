namespace Mundialito.Logic;

/// <summary>
/// A bet write the caller may not perform. Thrown, never caught by a blanket
/// <c>catch (Exception)</c>: a bare catch-all is what let a NullReferenceException be
/// reported to players as a validation error for months, invisible to server side Sentry
/// and to the 5xx metrics. Anything not derived from these propagates to a 500 with a stack
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

/// <summary>The bet exists but does not belong to the caller.</summary>
public class BetForbiddenException : Exception
{
    public BetForbiddenException(string message) : base(message) { }
}
