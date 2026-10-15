// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_notification_reenters : given.a_contributed_unit
{
    Exception _enrollmentError;
    Exception _commitError;

    void Establish() => _contributor.When(_ => _.Enrolled(Arg.Any<UnitOfWorkEnrollment>(), Arg.Any<ConcurrencyScope>())).Do(_ => Stage());

    async Task Because()
    {
        _enrollmentError = Catch.Exception(() => Stage());
        _commitError = await Catch.Exception(() => _unitOfWork.Commit());
    }

    [Fact] void should_refuse_mutation_from_notification() => _enrollmentError.ShouldBeOfExactType<UnitOfWorkMutatedDuringEnrollment>();
    [Fact] void should_prevent_commit_after_the_notification_failed() => _commitError.ShouldBeOfExactType<UnitOfWorkEnrollmentNotificationFailed>();
    [Fact] void should_not_append_any_event() => _eventSequence.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>());
    [Fact] void should_report_failure() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_completion_to_rollback() => _unitOfWork.IsCompleted.ShouldBeFalse();
}
