// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Validates antiforgery tokens for authenticated cookie requests that can mutate state.
/// Bearer-token calls, anonymous callers without an authentication cookie, and endpoints explicitly
/// marked to ignore antiforgery validation are unaffected.
/// </summary>
/// <param name="next">The next middleware.</param>
public class CookieAntiforgeryMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The Chronicle authentication cookie name.
    /// </summary>
    public const string AuthenticationCookieName = "Chronicle.Auth";

    /// <summary>
    /// The request header carrying the antiforgery token.
    /// </summary>
    public const string HeaderName = "X-CSRF-TOKEN";

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="antiforgery">The antiforgery service.</param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        var isAuthenticatedCookieRequest = context.User.Identity?.IsAuthenticated is true &&
            context.Request.Cookies.ContainsKey(AuthenticationCookieName);
        var ignoresAntiforgery = context.GetEndpoint()?.Metadata.GetMetadata<IgnoreAntiforgeryTokenAttribute>() is not null;

        if (isAuthenticatedCookieRequest && !ignoresAntiforgery && IsStateChanging(context.Request.Method))
        {
            try
            {
                await antiforgery.ValidateRequestAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Request protection validation failed."
                });
                return;
            }
        }

        await next(context);
    }

    static bool IsStateChanging(string method) =>
        !HttpMethods.IsGet(method) &&
        !HttpMethods.IsHead(method) &&
        !HttpMethods.IsOptions(method) &&
        !HttpMethods.IsTrace(method);
}
