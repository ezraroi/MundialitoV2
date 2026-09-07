using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Mundialito.DAL.Accounts;
using Mundialito.Models;

namespace Mundialito.Auth.Authorization;

/// <summary>
/// ASP.NET returns a 403 with an empty body when a policy is not met. The client has no way
/// to tell that apart from a network failure, so it used to show "the server is down" and
/// report an untitled, stackless event to Sentry.
///
/// This writes the same { "Message": "..." } shape the rest of the API uses, so the client
/// can display the real reason.
/// </summary>
public class ForbiddenMessageResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();
    private readonly ICurrentUserRoleProvider roleProvider;

    public ForbiddenMessageResultHandler(ICurrentUserRoleProvider roleProvider)
    {
        this.roleProvider = roleProvider;
    }

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Forbidden)
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var role = await roleProvider.GetCurrentRoleAsync();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorMessage { Message = MessageFor(role) }));
    }

    private static string MessageFor(Role? role) => role switch
    {
        Role.Disabled => "Your account is not approved yet. Please contact the admin.",
        null => "Your account could not be found. Please sign in again.",
        _ => "You do not have permission to perform this action."
    };
}
