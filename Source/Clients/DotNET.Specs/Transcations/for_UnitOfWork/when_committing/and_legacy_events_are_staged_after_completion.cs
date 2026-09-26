// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_legacy_events_are_staged_after_completion : given.a_unit_of_work
{
    Exception _eventError;
    Exception _batchError;

    async Task Because()
    {
        await _unitOfWork.Commit();
        _eventError = Record.Exception(() => _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown()));
        _batchError = Record.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new EventForEventSourceId("other", new object(), Causation.Unknown())],
            []));
    }

    [Fact] void should_preserve_legacy_event_enrollment() => _eventError.ShouldBeNull();
    [Fact] void should_preserve_legacy_batch_enrollment() => _batchError.ShouldBeNull();
    [Fact] void should_stage_both_events_as_before() => _unitOfWork.GetEvents().Count().ShouldEqual(2);
}
