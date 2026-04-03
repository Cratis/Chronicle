// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_constructing : Specification
{
    IEventLog _eventLog;
    EventAppendCollection _collection;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog, IObservableEventSequence>();
    }

    void Because() => _collection = new EventAppendCollection(_eventLog);

    [Fact] void should_subscribe_to_the_event_sequence() => (_eventLog as IObservableEventSequence)!.Received(1).Subscribe(Arg.Any<IObserveEventAppended>());
    [Fact] void should_subscribe_with_itself() => (_eventLog as IObservableEventSequence)!.Received(1).Subscribe(_collection);
}
