// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Records startup or shutdown once for a logical activation, optionally fanning out durably.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="ActivationId">The host-supplied activation.</param>
/// <param name="Kind">Startup or shutdown.</param>
/// <param name="AllNamespaces">Whether to snapshot existing namespaces.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record RecordLifecycleOccurrence(EventStoreName EventStore, string ActivationId, OccurrenceKind Kind, bool AllNamespaces = false)
{
    /// <summary>
    /// Dispatches to the event-store coordinator.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The durable result.</returns>
    public async Task<LifecycleOccurrenceResult> Handle(IGrainFactory grainFactory) => JsonSerializer.Deserialize<LifecycleOccurrenceResult>(await grainFactory.GetGrain<ILifecycleOccurrences>(EventStore.Value).Record(Kind, ActivationId, AllNamespaces))!;
}
