// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using DiagnosticsActivitySource = System.Diagnostics.ActivitySource;

namespace Orleans.Hosting;

/// <summary>
/// Extension methods for Chronicle meters.
/// </summary>
public static class ChronicleMetersExtensions
{
    /// <summary>
    /// Adds the Chronicle meters to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The <see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddChronicleMeters(this IServiceCollection services)
    {
        // Use open-generic keyed singleton registration so every IMeter<T> / IActivitySource<T>
        // injected with [FromKeyedServices(WellKnown.MeterName)] is resolved automatically.
        // Meter<T> and ActivitySource<T> carry [FromKeyedServices] on their constructors (Fundamentals
        // 7.10.3), so .NET 10 key-forwarding bridges the keyed Meter / ActivitySource into the wrapper.
        //
        // TODO: replace with services.AddNamedMeter(WellKnown.MeterName) / AddNamedActivitySource(...)
        //       once Cratis.Fundamentals ships DiagnosticsServiceCollectionExtensions.
        //       Tracked: https://github.com/Cratis/Fundamentals/issues — open an issue requesting
        //       a release that includes the named-registration convenience methods.
        services.TryAddKeyedSingleton(
            typeof(Meter),
            WellKnown.MeterName,
            static (_, key) => new Meter((string)key));
        services.TryAddKeyedSingleton(typeof(IMeter<>), WellKnown.MeterName, typeof(Meter<>));
        services.TryAddKeyedSingleton(
            typeof(DiagnosticsActivitySource),
            WellKnown.MeterName,
            static (_, key) => new DiagnosticsActivitySource((string)key));
        services.TryAddKeyedSingleton(typeof(IActivitySource<>), WellKnown.MeterName, typeof(ActivitySource<>));
        return services;
    }
}
