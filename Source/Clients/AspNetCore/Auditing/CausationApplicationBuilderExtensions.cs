// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for setting up causation.
/// </summary>
public static class CausationApplicationBuilderExtensions
{
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

            causationManager.Add(CausationType,
                ImmutableDictionary<string, string>.Empty);

            await next.Invoke();
        });

        return app;
    }
}
