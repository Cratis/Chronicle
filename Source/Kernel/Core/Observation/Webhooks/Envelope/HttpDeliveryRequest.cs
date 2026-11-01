// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Inputs to one live HTTP delivery attempt.
/// </summary>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="ObserverId">The observer.</param>
/// <param name="ObserverType">Reactor or reducer.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="Target">The webhook target.</param>
/// <param name="Partition">The partition key.</param>
/// <param name="Events">The events in sequence order.</param>
/// <param name="DeliverBy">The required execution deadline.</param>
/// <param name="AccessToken">Optional authorization token.</param>
public record HttpDeliveryRequest(EventStoreName EventStore, EventStoreNamespaceName Namespace, ObserverId ObserverId, string ObserverType, EventSequenceId EventSequenceId, WebhookTarget Target, Key Partition, IReadOnlyList<AppendedEvent> Events, DateTimeOffset DeliverBy, string? AccessToken = null);
