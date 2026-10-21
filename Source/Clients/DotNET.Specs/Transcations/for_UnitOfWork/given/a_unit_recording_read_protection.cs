// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_recording_read_protection : a_unit_of_work
{
    protected bool _readProtectionCalled;
    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequence, IEventSequenceWithReadProtection>();
        _eventStore.GetEventSequence(EventSequenceId.Log).Returns(_eventSequence);
        _unitOfWork = new(_correlationId, OnUnitOfWorkCompleted, _eventStore);
        _eventSequence.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(call =>
        {
            _eventsAppended = call.Arg<IEnumerable<EventForEventSourceId>>();
            _concurrencyScopesAppended = call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>();
            return _appendResult;
        });
        ((IEventSequenceWithReadProtection)_eventSequence).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<IEnumerable<NamedTag>>(), Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>(), Arg.Any<ReadProtectionCommit>()).Returns(_ =>
        {
            _readProtectionCalled = true;
            return _appendResult;
        });
    }
}
