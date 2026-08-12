// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_an_ordered_batch : given.a_unit_of_work
{
    void Establish() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new(EventSourceId.New(), new SomeEvent())],
        [new(EventSourceId.New(), ConcurrencyScope.None)]);

    async Task Because() => await _unitOfWork.Rollback();

    [Fact] void should_not_append_the_batch() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_not_leave_events_in_the_unit_of_work() => _unitOfWork.GetEvents().ShouldBeEmpty();

    record SomeEvent();
}
