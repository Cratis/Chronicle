// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The version-one delivery envelope. Runtime delivery is live-only.
/// </summary>
/// <param name="Kind">The envelope kind.</param>
/// <param name="ObserverType">Reactor or reducer.</param>
/// <param name="ObserverId">The observer identifier.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="DeliveryId">The stable delivery identifier.</param>
/// <param name="Batches">The partition batches.</param>
/// <param name="Replay">Reserved replay flag; false in v1.</param>
/// <param name="ReplayEpoch">Reserved replay epoch; null in v1.</param>
public record DeliveryEnvelope(
    [property: JsonPropertyName("kind")] DeliveryKind Kind,
    [property: JsonPropertyName("observerType")] string ObserverType,
    [property: JsonPropertyName("observerId")] ObserverId ObserverId,
    [property: JsonPropertyName("eventStore")] EventStoreName EventStore,
    [property: JsonPropertyName("namespace")] EventStoreNamespaceName Namespace,
    [property: JsonPropertyName("eventSequenceId")] EventSequenceId EventSequenceId,
    [property: JsonPropertyName("deliveryId")] DeliveryId DeliveryId,
    [property: JsonPropertyName("batches")] IReadOnlyList<DeliveryBatch> Batches,
    [property: JsonPropertyName("replay"), JsonIgnore(Condition = JsonIgnoreCondition.Never)] bool Replay = false,
    [property: JsonPropertyName("replayEpoch"), JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? ReplayEpoch = null);
