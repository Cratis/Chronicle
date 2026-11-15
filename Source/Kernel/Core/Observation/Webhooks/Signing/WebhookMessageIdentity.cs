// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents everything that identifies one webhook message, the input of <see cref="WebhookMessageIds"/>.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the observer belongs to.</param>
/// <param name="ObserverId">The <see cref="ObserverId"/> of the webhook observer.</param>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/> the events come from.</param>
/// <param name="Kind">The kind of message, for example <see cref="WebhookMessageIdentity.EventsKind"/>.</param>
/// <param name="Batches">The partition ranges the message carries.</param>
/// <param name="ReplayEpoch">The <see cref="ReplayEpoch"/> when a replay made the delivery, otherwise null.</param>
public record WebhookMessageIdentity(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    ObserverId ObserverId,
    EventSequenceId EventSequenceId,
    string Kind,
    IReadOnlyList<WebhookMessageBatch> Batches,
    ReplayEpoch? ReplayEpoch)
{
    /// <summary>
    /// The kind of a message that delivers events.
    /// </summary>
    public const string EventsKind = "events";

    /// <summary>
    /// Creates the identity of a message delivering the events of one partition.
    /// </summary>
    /// <param name="key">The <see cref="ObserverKey"/> of the observer.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="events">The events delivered, in order.</param>
    /// <param name="replayEpoch">The replay operation, or null.</param>
    /// <returns>The <see cref="WebhookMessageIdentity"/>.</returns>
    public static WebhookMessageIdentity ForEvents(ObserverKey key, Key partition, IReadOnlyList<AppendedEvent> events, ReplayEpoch? replayEpoch)
    {
        var first = events.Count == 0 ? EventSequenceNumber.Unavailable : events[0].Context.SequenceNumber;
        var last = events.Count == 0 ? EventSequenceNumber.Unavailable : events[^1].Context.SequenceNumber;
        return new(key.EventStore, key.Namespace, key.ObserverId, key.EventSequenceId, EventsKind, [new WebhookMessageBatch(partition, first, last)], replayEpoch);
    }
}
