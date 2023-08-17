// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Collections;
using Aksio.Cratis.Auditing;
using Aksio.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for setting up causation.
/// </summary>
public static class CausationApplicationBuilderExtensions
{
    /// <summary>
    /// The causation type for ASP.NET requests.
    /// </summary>
    public static readonly CausationType CausationType = new("ASP.NET Request");

    /// <summary>
    /// Use causation.
    /// </summary>
    /// <param name="app"><see cref="IApplicationBuilder"/> to extend.</param>
    /// <returns><see cref="IApplicationBuilder"/> for continuation.</returns>
    public static IApplicationBuilder UseCausation(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var causationManager = context.RequestServices.GetRequiredService<ICausationManager>();
            var properties = new Dictionary<string, string>
                {
                    { "route", context.Request.Path },
                    { "method", context.Request.Method },
                    { "host", context.Request.Host.Value },
                    { "protocol", context.Request.Protocol },
                    { "scheme", context.Request.Scheme },
                    { "query", context.Request.QueryString.ToString() },
                };
            context.Request.RouteValues.ForEach(_ => properties.Add($"route-value:{_.Key}", _.Value?.ToString() ?? string.Empty));
            causationManager.Add(CausationType, properties);

            await next.Invoke();
        });

        return app;
    }
}
