// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_earlier_events_target_another_sequence : given.a_contributed_unit
{
    object _earlierEvent;
    Exception _error;

    void Establish()
    {
        _earlierEvent = new object();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(ConcurrencyScope.NotSet);
        _unitOfWork.AddEvent(EventSequenceId.Outbox, EventSourceId.New(), _earlierEvent, Causation.Unknown());
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_required);
    }

    void Because() => _error = Catch.Exception(() => Stage());

    [Fact] void should_reject_a_requirement_for_a_different_sequence() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_keep_only_the_earlier_event() => _unitOfWork.GetEvents().ShouldContainOnly(_earlierEvent);
}
