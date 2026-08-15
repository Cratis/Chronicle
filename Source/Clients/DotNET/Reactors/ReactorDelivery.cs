// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Represents the identity of the delivery of one event to one reactor partition.
/// </summary>
/// <param name="Reactor">The <see cref="ReactorId"/> of the reactor the event is delivered to.</param>
/// <param name="EventStore">The <see cref="EventStoreName"/> the event belongs to.</param>
/// <param name="Namespace">The <see cref="EventStoreNamespaceName"/> the event belongs to.</param>
/// <param name="EventSequence">The <see cref="EventSequenceId"/> the reactor observes.</param>
/// <param name="Partition">The <see cref="EventSourceId"/> partition the event is observed in.</param>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> of the event within the event sequence.</param>
/// <remarks>
/// Declare a parameter of this type on a handler method and Chronicle passes it in, the same way it passes the
/// <see cref="EventContext"/>. Every component is something Chronicle already knows about the delivery, so the
/// identity survives everything that re-delivers an event: recovering a failed partition re-reads the same
/// partition from the sequence number it failed on, and a replay re-reads the same sequence numbers for the
/// same observer. Both therefore produce the same <see cref="Id"/> as the delivery they repeat.
/// <para>
/// <b>What it does not do.</b> It is an identity, not a guarantee. Chronicle does not know whether your side
/// effect ran, so it cannot suppress a repeat on your behalf and this is not exactly-once delivery. Recording
/// the identity and checking it before the effect narrows at-least-once to at-most-once <i>only</i> as far as
/// your own record is atomic with the effect - a process that dies between the effect and the record still
/// repeats the effect on the next delivery.
/// </para>
/// <para>
/// Compare with <see cref="OnceOnlyAttribute"/> and <see cref="ReplayAttribute"/>, which act on replay alone and
/// need no storage. This is the seam for the case they do not cover: the ordinary re-delivery that follows a
/// failed partition being recovered.
/// </para>
/// </remarks>
public record ReactorDelivery(
    ReactorId Reactor,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    EventSequenceId EventSequence,
    EventSourceId Partition,
    EventSequenceNumber SequenceNumber)
{
    static readonly ConcurrentDictionary<(Type ReactorType, string EventStore), EventSequenceId> _eventSequenceIdCache = new();

    /// <summary>
    /// Gets the stable identity of this delivery.
    /// </summary>
    /// <remarks>
    /// Use this as the key of whatever records that the side effect completed. It is derived from the record's
    /// components, so two <see cref="ReactorDelivery"/> instances that are equal always carry the same value.
    /// </remarks>
    public DeliveryId Id => new(KeyHelper.Combine(Reactor, EventStore, Namespace, EventSequence, Partition, SequenceNumber));

    /// <summary>
    /// Creates the <see cref="ReactorDelivery"/> for an event being delivered to a reactor instance.
    /// </summary>
    /// <param name="reactor">The reactor instance the event is delivered to.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> of the event being delivered.</param>
    /// <returns>The <see cref="ReactorDelivery"/> identifying the delivery.</returns>
    /// <exception cref="Validators.TypeMustImplementReactor">Thrown when <paramref name="reactor"/> is not a reactor.</exception>
    public static ReactorDelivery For(object reactor, EventContext eventContext)
    {
        var reactorType = reactor.GetType();
        return For(reactorType.GetReactorId(), GetEventSequenceIdFor(reactorType, eventContext.EventStore), eventContext);
    }

    /// <summary>
    /// Creates the <see cref="ReactorDelivery"/> for an event being delivered to a known reactor and event sequence.
    /// </summary>
    /// <param name="reactor">The <see cref="ReactorId"/> the event is delivered to.</param>
    /// <param name="eventSequence">The <see cref="EventSequenceId"/> the reactor observes.</param>
    /// <param name="eventContext">The <see cref="EventContext"/> of the event being delivered.</param>
    /// <returns>The <see cref="ReactorDelivery"/> identifying the delivery.</returns>
    public static ReactorDelivery For(ReactorId reactor, EventSequenceId eventSequence, EventContext eventContext) =>
        new(
            reactor,
            eventContext.EventStore,
            eventContext.Namespace,
            eventSequence,
            eventContext.EventSourceId,
            eventContext.SequenceNumber);

    static EventSequenceId GetEventSequenceIdFor(Type reactorType, EventStoreName eventStore) =>
        _eventSequenceIdCache.GetOrAdd(
            (reactorType, eventStore.Value),
            static key => key.ReactorType.GetEventSequenceId(key.EventStore));
}
