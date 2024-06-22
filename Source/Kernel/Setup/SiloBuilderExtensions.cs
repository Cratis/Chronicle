// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains;
using Cratis.Chronicle.Grains.Observation.Placement;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Setup.Serialization;

namespace Orleans.Hosting;

/// <summary>
/// Defines extensions for <see cref="ISiloBuilder"/> for configuring Chronicle in the current silo.
/// </summary>
public static class SiloBuilderExtensions
{
    /// <summary>
    /// Add Chronicle to the silo. This enables running Chronicle in process in the same process as the silo.
    /// </summary>
    /// <param name="builder">The <see cref="ISiloBuilder"/> to add to.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicleToSilo(this ISiloBuilder builder)
    {
        builder
            .UseLocalhostClustering() // TODO: Implement MongoDB clustering
            .AddEventSequenceStreaming()
            .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.ProjectionChanged, _ => _.FireAndForgetDelivery = true)
            .AddReplayStateManagement()
            .AddReminders()
            .AddTelemetry()
            .AddMemoryGrainStorage("PubSubStore") // TODO: Store Grain state in Database
            .AddStorageProviders()
            .ConfigureCpuBoundWorkers()
            .ConfigureSerialization();

        return builder;
    }
}
