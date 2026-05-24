// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Services.Observation.Reactors;
using Cratis.Chronicle.Services.Observation.Reducers;
using Cratis.Metrics;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddChronicleMeter();
        services.AddMeter<EventSequence>();
        services.AddMeter<AppendedEventsQueue>();
        services.AddMeter<Observer>();
        services.AddMeter<ConnectedClients>();

        services.AddChronicleActivitySource();
        services.AddActivitySource<EventSequence>();
        services.AddActivitySource<AppendedEventsQueue>();
        services.AddActivitySource<Observer>();
        services.AddActivitySource<Reducers>();
        services.AddActivitySource<Reactors>();

        return services;
    }

    static IServiceCollection AddMeter<TTarget>(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IMeter<TTarget>>(WellKnown.MeterName, (sp, key) =>
        {
            var meter = sp.GetRequiredKeyedService<Meter>(key);
            return new Meter<TTarget>(meter);
        });

        return services;
    }

    static IServiceCollection AddActivitySource<TTarget>(this IServiceCollection services)
    {
        services.AddKeyedSingleton<IActivitySource<TTarget>>(WellKnown.MeterName, (sp, key) =>
        {
            var activitySource = sp.GetRequiredKeyedService<ActivitySource>(key);
            return new ActivitySource<TTarget>(activitySource);
        });

        return services;
    }
}
