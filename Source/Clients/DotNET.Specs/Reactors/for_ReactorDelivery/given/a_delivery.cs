// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorDelivery.given;

public class a_delivery : Specification
{
    protected const string EventStore = "orders";
    protected const string Namespace = "default";
    protected static readonly EventSourceId _partition = "order-42";
    protected static readonly EventSequenceNumber _sequenceNumber = 7UL;

    protected OrderConfirmations _reactor;
    protected EventContext _context;
    protected ReactorDelivery _delivery;

    void Establish()
    {
        _reactor = new OrderConfirmations();
        _context = ContextFor(EventStore, Namespace, _partition, _sequenceNumber);
        _delivery = ReactorDelivery.For(_reactor, _context);
    }

    /// <summary>
    /// Builds the context an event arrives in, as a fresh instance every time, so that a spec comparing two
    /// deliveries proves the identity is derived from the values rather than shared through one object.
    /// </summary>
    /// <param name="eventStore">The event store the event belongs to.</param>
    /// <param name="namespace">The namespace the event belongs to.</param>
    /// <param name="partition">The partition the event is observed in.</param>
    /// <param name="sequenceNumber">The sequence number of the event.</param>
    /// <returns>The <see cref="EventContext"/> for the event.</returns>
    protected static EventContext ContextFor(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSourceId partition,
        EventSequenceNumber sequenceNumber) =>
        EventContext.From(
            eventStore,
            @namespace,
            typeof(OrderPlaced).GetEventType(),
            EventSourceType.Default,
            partition,
            EventStreamType.All,
            EventStreamId.Default,
            sequenceNumber,
            CorrelationId.NotSet);
}
