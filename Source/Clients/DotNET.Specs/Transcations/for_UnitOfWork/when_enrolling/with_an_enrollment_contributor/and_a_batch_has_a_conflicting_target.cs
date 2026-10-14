// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_batch_has_a_conflicting_target : given.a_contributed_unit
{
    EventSourceId _other;
    Exception _error;

    void Establish()
    {
        _other = EventSourceId.New();
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(call => new ConcurrencyScope(0UL, call.Arg<UnitOfWorkEnrollment>().EventSourceId));
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new(_source, _event), new(_other, new object())],
        [new(_other, ConcurrencyScope.None)]));

    [Fact] void should_reject_the_conflicting_batch() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_stage_nothing_from_the_batch() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_notify_any_target() => _contributor.DidNotReceive().Enrolled(Arg.Any<UnitOfWorkEnrollment>(), Arg.Any<ConcurrencyScope>());
}
