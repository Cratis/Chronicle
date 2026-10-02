// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy;

public class given_an_event_sequence : Specification
{
    protected IEventSequence _eventSequence;
    protected OptimisticConcurrencyStrategy _strategy;
    protected EventSourceId _eventSourceId = "source";

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence>();
        _eventSequence
            .GetTailSequenceNumber(
                Arg.Any<EventSourceId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(new EventSequenceNumber(42));
        _strategy = new OptimisticConcurrencyStrategy(_eventSequence);
    }
}
