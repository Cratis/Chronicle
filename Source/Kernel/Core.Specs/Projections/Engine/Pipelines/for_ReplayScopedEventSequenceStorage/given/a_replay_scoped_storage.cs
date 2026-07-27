// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage.given;

public class a_replay_scoped_storage : Specification
{
    protected IEventSequenceStorage _inner;
    protected IEventSequenceStorage _storage;
    protected IReplayScopedCache _session;
    protected EventSourceId _eventSourceId;
    protected EventTypeId[] _eventTypeIds;
    protected EventType[] _eventTypes;

    void Establish()
    {
        _inner = Substitute.For<IEventSequenceStorage>();
        var decorator = new ReplayScopedEventSequenceStorage(_inner);
        _storage = decorator;
        _session = decorator;

        _eventSourceId = "the-parent";
        _eventTypeIds = [new EventTypeId("parent-event")];
        _eventTypes = [new EventType("parent-event", EventTypeGeneration.First)];

        _inner.TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Task.FromResult(Option<AppendedEvent>.None()));
        _inner.GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(Task.FromResult(EventSequenceNumber.First));
    }
}
