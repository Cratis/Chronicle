// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Records durable application or lifecycle work; null namespace selects application scope.
/// </summary>
/// <param name="EventStore">The target store.</param>
/// <param name="Namespace">The namespace or null for application scope.</param>
/// <param name="Trigger">The registered trigger.</param>
/// <param name="IdempotencyId">The retry identity.</param>
/// <param name="Payload">The plaintext JSON payload.</param>
/// <param name="Kind">The ingress kind.</param>
/// <param name="ApplicationScope">Selects event-store application storage independently of namespace encoding.</param>
[Command]
[BelongsTo(WellKnownServices.Occurrences)]
public record RecordOccurrence(EventStoreName EventStore, string? Namespace, string Trigger, string IdempotencyId, string Payload, OccurrenceKind Kind = OccurrenceKind.Application, bool ApplicationScope = false)
{
    /// <summary>
    /// Records through the queue's serialized ingress.
    /// </summary>
    /// <param name="grainFactory">The grain factory.</param>
    /// <returns>The durable outcome.</returns>
    public Task<OccurrenceIngressResult> Handle(IGrainFactory grainFactory) => OccurrenceQueueKey.Resolve(grainFactory, EventStore, ApplicationScope ? null : Namespace, Trigger).Record(IdempotencyId, Payload, Kind);
}
