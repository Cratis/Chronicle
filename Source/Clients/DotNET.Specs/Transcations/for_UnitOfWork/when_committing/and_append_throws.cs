// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_append_throws : given.a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it
{
    Exception _thrownDuringAppend;
    Exception _result;

    void Establish()
    {
        _thrownDuringAppend = new InvalidOperationException("append failed");
        _eventSequence
            .AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                Arg.Any<CorrelationId?>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Throws(_thrownDuringAppend);
    }

    async Task Because() => _result = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_propagate_the_exception() => _result.ShouldEqual(_thrownDuringAppend);
    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_be_completed() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
