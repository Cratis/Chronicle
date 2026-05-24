// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;
using DiagnosticsActivitySource = System.Diagnostics.ActivitySource;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding Chronicle activity sources to an <see cref="IServiceCollection"/>.
/// </summary>
internal static class ChronicleActivitySourceExtensions
{
    /// <summary>
    /// Adds the Chronicle client <see cref="DiagnosticsActivitySource"/> as a keyed singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddChronicleActivitySource(this IServiceCollection services)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        services.AddKeyedSingleton(ClientActivity.SourceName, new DiagnosticsActivitySource(ClientActivity.SourceName));
#pragma warning restore CA2000 // Dispose objects before losing scope
        return services;
    }

    /// <summary>
    /// Adds an <see cref="IActivitySource{TTarget}"/> keyed singleton backed by the shared Chronicle client <see cref="DiagnosticsActivitySource"/>.
    /// </summary>
    /// <typeparam name="TTarget">The type the activity source is for.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddActivitySource<TTarget>(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IActivitySource<TTarget>>(ClientActivity.SourceName, (sp, key) =>
        {
            var source = sp.GetRequiredKeyedService<DiagnosticsActivitySource>(key);
            return new ActivitySource<TTarget>(source);
        });

        return services;
    }
}
