// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DiagnosticsActivitySource = System.Diagnostics.ActivitySource;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Compatibility extensions providing <c>AddNamedMeter</c> and <c>AddNamedActivitySource</c> while
/// <c>Cratis.Fundamentals</c> does not yet publish <c>DiagnosticsServiceCollectionExtensions</c>.
/// See: https://github.com/Cratis/Fundamentals/blob/main/Source/DotNET/Fundamentals/DependencyInjection/DiagnosticsServiceCollectionExtensions.cs.
/// Remove this file once the upstream package exposes these methods.
/// </summary>
static class NamedDiagnosticsServiceCollectionExtensions
{
    /// <summary>
    /// Adds a named <see cref="Meter"/> and keyed typed <see cref="IMeter{T}"/> registrations.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="name">Name of the meter.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddNamedMeter(this IServiceCollection services, string name)
    {
        services.TryAddKeyedSingleton(typeof(Meter), name, static (_, key) => new Meter((string)key));
        services.TryAddKeyedSingleton(typeof(IMeter<>), name, typeof(Meter<>));
        return services;
    }

    /// <summary>
    /// Adds a named <see cref="DiagnosticsActivitySource"/> and keyed typed <see cref="IActivitySource{T}"/> registrations.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="name">Name of the activity source.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    internal static IServiceCollection AddNamedActivitySource(this IServiceCollection services, string name)
    {
        services.TryAddKeyedSingleton(typeof(DiagnosticsActivitySource), name, static (_, key) => new DiagnosticsActivitySource((string)key));
        services.TryAddKeyedSingleton(typeof(IActivitySource<>), name, typeof(ActivitySource<>));
        return services;
    }
}
