// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Claims due occurrences for a connected worker.
/// </summary>
/// <param name="EventStore">The target store.</param>
/// <param name="Namespace">The namespace or null for application scope.</param>
/// <param name="Trigger">The trigger.</param>
/// <param name="WorkerId">The connection identity expired on disconnect.</param>
/// <param name="LeaseSeconds">The duration, zero for 60 seconds, capped at 600.</param>
/// <param name="Max">The requested claim count.</param>
/// <param name="ApplicationScope">Selects event-store application storage.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record ClaimOccurrences(EventStoreName EventStore, string? Namespace, string Trigger, string WorkerId, int LeaseSeconds = 0, int Max = 1, bool ApplicationScope = false)
{
    /// <summary>
    /// Claims through the trigger queue.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The durable claims and their fencing epochs.</returns>
    public async Task<OccurrenceClaims> Handle(IGrainFactory grainFactory) => new(OccurrenceClaimConverters.FromJson(
        await OccurrenceQueueKey.Resolve(grainFactory, EventStore, ApplicationScope ? null : Namespace, Trigger).Claim(WorkerId, LeaseSeconds, Max)));
}
