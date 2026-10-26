// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        services.AddNamedMeter(WellKnown.MeterName);
        services.AddNamedActivitySource(WellKnown.MeterName);

        // Publishes the legacy metric names next to the convention names for one minor release.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, LegacyMetricNameBridgeHostedService>());

        for (var index = services.Count - 1; index >= 0; index--)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType == typeof(IActivitySource<>) &&
                Equals(descriptor.ServiceKey, WellKnown.MeterName))
            {
                services.RemoveAt(index);
                break;
            }
        }

        services.AddKeyedSingleton(typeof(IActivitySource<>), WellKnown.MeterName, typeof(KeyedActivitySource<>));
        return services;
    }

    sealed class KeyedActivitySource<T>(IServiceProvider serviceProvider, [ServiceKey] string? key = null) : IActivitySource<T>
    {
        public ActivitySource ActualSource { get; } = key is null
            ? new(typeof(T).FullName ?? typeof(T).Name)
            : serviceProvider.GetRequiredKeyedService<ActivitySource>(key);
    }
}
