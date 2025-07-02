// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_has_concurrency_violations : given.a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it
{
    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
        ConcurrencyViolations = new Dictionary<EventSourceId, ConcurrencyViolation>
            {
                {
                    "event-source-id-1",
                    new ConcurrencyViolation(42ul, 43ul)
                },
                {
                    "event-source-id-2",
                    new ConcurrencyViolation(44ul, 45ul)
                }
            },
    };

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_have_concurrency_violations() => _unitOfWork.GetConcurrencyViolations().ShouldContainOnly(_appendResult.ConcurrencyViolations);
    [Fact] void should_not_be_successful() => _unitOfWork.IsSuccess.ShouldBeFalse();
}
