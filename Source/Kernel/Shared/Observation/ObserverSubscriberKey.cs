// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a key for an observer.
/// </summary>
/// <param name="ObserverId">The unique identifier of the observer.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="EventSequenceId">The event sequence.</param>
/// <param name="EventSourceId">The event source identifier - partition.</param>
/// <param name="SiloAddress">The string representation of the address to the Orleans Silo the subscriber grain should be running on.</param>
/// <remarks>
/// The silo address must match the silo that is typically hosting the actual receiver of events.
/// This is to avoid network hops when sending events to an observer, e.g. a client.
/// </remarks>
public record ObserverSubscriberKey(
    ObserverId ObserverId,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequenceId,
    EventSourceId EventSourceId,
    string SiloAddress)
{
    /// <summary>
    /// Implicitly convert from <see cref="ObserverKey"/> to string.
    /// </summary>
    /// <param name="key"><see cref="ObserverKey"/> to convert from.</param>
    public static implicit operator string(ObserverSubscriberKey key) => key.ToString();

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{ObserverId}+{EventStore}+{Namespace}+{EventSequenceId}+{EventSourceId}+{SiloAddress}";
    }

    /// <summary>
    /// Parse a key into its components.
    /// </summary>
    /// <param name="key">Key to parse.</param>
    /// <returns>Parsed <see cref="ObserverKey"/> instance.</returns>
    public static ObserverSubscriberKey Parse(string key)
    {
        var elements = key.Split('+');
        var observerId = (ObserverId)elements[0];
        var eventStore = (EventStoreName)elements[1];
        var @namespace = (EventStoreNamespaceName)elements[2];
        var eventSequenceId = (EventSequenceId)elements[3];
        var eventSourceId = (EventSourceId)elements[4];
        var siloAddress = elements[4];

        return new(observerId, eventStore, @namespace, eventSequenceId, eventSourceId, siloAddress);
    }

    /// <summary>
    /// Creates an ObserverSubscriberKey from an ObserverKey and an EventSourceId.
    /// </summary>
    /// <param name="observerKey">The Observer Subscriber Key.</param>
    /// <param name="eventSourceId">The EventSourceId.</param>
    /// <param name="siloAddress">Name of the silo it should run on.</param>
    /// <returns>An ObserverSubscriber Key.</returns>
    public static ObserverSubscriberKey FromObserverKey(ObserverKey observerKey, EventSourceId eventSourceId, string siloAddress)
        => new(observerKey.ObserverId, observerKey.EventStore, observerKey.Namespace, observerKey.EventSequenceId, eventSourceId, siloAddress);
}
