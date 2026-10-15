// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Completes an occurrence under its fencing epoch.
/// </summary>
/// <param name="EventStore">The target store.</param>
/// <param name="Namespace">The namespace or null for application scope.</param>
/// <param name="Trigger">The trigger.</param>
/// <param name="OccurrenceId">The occurrence identity.</param>
/// <param name="Epoch">The claimed epoch.</param>
/// <param name="ApplicationScope">Selects event-store application storage.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record CompleteOccurrence(EventStoreName EventStore, string? Namespace, string Trigger, string OccurrenceId, long Epoch, bool ApplicationScope = false)
{
    /// <summary>
    /// Completes only the current epoch, idempotently.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The fenced outcome.</returns>
    public async Task<OccurrenceTransitionResult> Handle(IGrainFactory grainFactory) => new(await OccurrenceQueueKey.Resolve(grainFactory, EventStore, ApplicationScope ? null : Namespace, Trigger).Complete(OccurrenceId, Epoch));
}
