// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy.given;

public class an_optimistic_concurrency_strategy : Specification
{
    protected OptimisticConcurrencyStrategy _strategy;
    protected IEventSequence _eventSequence;
    protected EventSourceId _eventSourceId;
    protected EventSequenceNumber _tail;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _tail = 42UL;
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence.GetTailSequenceNumber(
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(_tail);

        _strategy = new OptimisticConcurrencyStrategy(_eventSequence);
    }
}
