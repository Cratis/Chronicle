// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_ordered_batch_then_a_blank_legacy_target : given.a_unit_of_work
{
    OrderedEvent _orderedEvent;
    Exception _error;

    void Establish()
    {
        _orderedEvent = new();
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), _orderedEvent)],
            []);
    }

    async Task Because()
    {
        _error = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Log,
            new EventSourceId(" "),
            new RejectedLegacyEvent(),
            Causation.Unknown()));
        await _unitOfWork.Commit();
    }

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeLabelMustBeSpecified>();
    [Fact] void should_not_stage_the_legacy_event() => _unitOfWork.GetEvents().ShouldContainOnly(_orderedEvent);
    [Fact] void should_append_only_the_ordered_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_orderedEvent);

    record OrderedEvent;
    record RejectedLegacyEvent;
}
