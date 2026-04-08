// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.XUnit.Integration.for_EventAppendCollection.given;

public class an_event_append_collection : Specification
{
    protected Subject<IEnumerable<AppendedEventWithResult>> _subject;
    protected IEventLog _eventLog;
    protected EventAppendCollection _collection;

    void Establish()
    {
        _subject = new Subject<IEnumerable<AppendedEventWithResult>>();
        _eventLog = Substitute.For<IEventLog>();
        _eventLog.AppendOperations.Returns(_subject);
        _collection = new EventAppendCollection(_eventLog);
    }

    protected static AppendedEventWithResult MakeAppendedEvent(
        CorrelationId correlationId,
        EventSourceId eventSourceId,
        object @event,
        IEnumerable<Causation> causation,
        AppendResult result)
    {
        var context = EventContext.Empty with
        {
            EventSourceId = eventSourceId,
            Causation = causation
        };
        return new AppendedEventWithResult(new AppendedEvent(context, @event), result);
    }

    protected void FireEvent(ulong sequenceNumber = 7)
    {
        var correlationId = CorrelationId.New();
        _subject.OnNext(
        [
            MakeAppendedEvent(
                correlationId,
                EventSourceId.New(),
                new object(),
                [],
                AppendResult.Success(correlationId, new EventSequenceNumber(sequenceNumber)))
        ]);
    }
}
