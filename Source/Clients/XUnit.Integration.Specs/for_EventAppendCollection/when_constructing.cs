// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection;

public class when_constructing : Specification
{
    IEventLog _eventLog;
    Subject<IEnumerable<AppendedEventWithResult>> _subject;
    EventAppendCollection _collection;

    void Establish()
    {
        _subject = new Subject<IEnumerable<AppendedEventWithResult>>();
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.AppendOperations.Returns(_subject);
    }

    void Because() => _collection = new EventAppendCollection(_eventLog);

    [Fact] void should_collect_events_pushed_through_the_observable() {
        var correlationId = CorrelationId.New();
        _subject.OnNext([new AppendedEventWithResult(
            new AppendedEvent(EventContext.Empty, new object()),
            AppendResult.Success(correlationId, EventSequenceNumber.First))]);
        _collection.All.Count.ShouldEqual(1);
    }
}
