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
    /// <param name="configure">Optional delegate for configuring the <see cref="IChronicleBuilder"/>.</param>
    /// <returns><see cref="ISiloBuilder"/> for continuation.</returns>
    public static ISiloBuilder AddChronicleToSilo(this ISiloBuilder builder, Action<IChronicleBuilder>? configure = default)
    {
        builder
            .AddPlacementDirector<ConnectedObserverPlacementStrategy, ConnectedObserverPlacementDirector>()
            .AddBroadcastChannel(WellKnownBroadcastChannelNames.NamespaceAdded, _ => _.FireAndForgetDelivery = true)
            .AddReplayStateManagement()
            .AddReminders()
            .AddTelemetry()
            .AddMemoryGrainStorage("PubSubStore") // TODO: Store Grain state in Database
            .AddStorageProviders()
            .ConfigureCpuBoundWorkers()
            .ConfigureSerialization();

        var chronicleBuilder = new ChronicleBuilder(builder.Services, builder.Configuration);
        configure?.Invoke(chronicleBuilder);
        return builder;
    }
}
