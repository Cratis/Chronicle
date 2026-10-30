// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.EventStores;

/// <summary>
/// Represents the stored deployment generation and latest registered fingerprint for an event store.
/// </summary>
/// <param name="Generation">The highest accepted generation, or 0 when none was sent.</param>
/// <param name="Fingerprint">The latest fingerprint, or empty before registration.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventStores)]
public record EventStoreDeployment(ulong Generation, string Fingerprint)
{
    /// <summary>
    /// Gets the stored deployment state for an event store.
    /// </summary>
    /// <param name="eventStore">The event store name.</param>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The stored deployment state.</returns>
    public static async Task<EventStoreDeployment> GetEventStoreDeployment(string eventStore, IGrainFactory grainFactory)
    {
        var state = await grainFactory.GetGrain<IEventStoreDeploymentRegistry>(eventStore).Get();
        return new(state.Generation.Value, state.Fingerprint.Value);
    }
}
