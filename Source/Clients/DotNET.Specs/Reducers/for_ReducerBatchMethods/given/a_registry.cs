// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reducers.for_ReducerBatchMethods.given;

public class a_registry : Specification
{
    protected IEventTypes _eventTypes;
    protected BatchContext _context;

    void Establish()
    {
        _eventTypes = Substitute.For<IEventTypes>();
        _eventTypes.AllClrTypes.Returns(new[] { typeof(Deposited), typeof(Withdrawn) }.ToImmutableList());
        _eventTypes.GetEventTypeFor(typeof(Deposited)).Returns(new EventType("deposited", 1));
        _eventTypes.GetEventTypeFor(typeof(Withdrawn)).Returns(new EventType("withdrawn", 1));
        _context = new("accounts", "store", "ns", EventSequenceId.Log, ObservationOrigin.CatchUp, [new EventSourceId("account")], 2, 1UL, 2UL, null, CancellationToken.None);
    }

    protected static EventAndContext Event(object @event, ulong sequenceNumber) =>
        new(@event, EventContext.Empty with { EventSourceId = "account", SequenceNumber = sequenceNumber });

    public record Deposited(decimal Amount);

    public record Withdrawn(decimal Amount);
}
