// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Records the immediate peer of every request before anything else in the pipeline runs.
/// </summary>
/// <remarks>
/// It must be the first startup filter, registered ahead of the one ASP.NET Core adds for forwarded headers,
/// so that the address it records is the one the connection was accepted from. Use <see cref="Register"/>.
/// </remarks>
public class ImmediatePeerStartupFilter : IStartupFilter
{
    /// <summary>
    /// Registers the filter ahead of every other startup filter.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void Register(IServiceCollection services) =>
        services.Insert(0, ServiceDescriptor.Transient<IStartupFilter, ImmediatePeerStartupFilter>());

    /// <summary>
    /// Records the immediate peer on the request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public static void Capture(HttpContext context) =>
        context.Features.Set<IImmediatePeerFeature>(new ImmediatePeerFeature(context.Connection.RemoteIpAddress));

    /// <inheritdoc/>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use((context, nextMiddleware) =>
        {
            Capture(context);
            return nextMiddleware(context);
        });
        next(app);
    };
}
