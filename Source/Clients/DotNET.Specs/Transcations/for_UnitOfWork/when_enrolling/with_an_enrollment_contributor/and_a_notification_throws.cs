// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_notification_throws : given.a_contributed_unit
{
    Exception _notificationFailure;
    Exception _enrollmentError;
    Exception _commitError;

    void Establish()
    {
        _notificationFailure = new UnitOfWorkMutatedDuringEnrollment();
        _contributor.When(_ => _.Enrolled(Arg.Any<UnitOfWorkEnrollment>(), Arg.Any<ConcurrencyScope>())).Do(_ => throw _notificationFailure);
    }

    async Task Because()
    {
        _enrollmentError = Catch.Exception(() => Stage());
        _commitError = await Catch.Exception(() => _unitOfWork.Commit());
        await _unitOfWork.Rollback();
    }

    [Fact] void should_propagate_the_notification_failure() => _enrollmentError.ShouldEqual(_notificationFailure);
    [Fact] void should_fail_closed_on_commit() => _commitError.ShouldBeOfExactType<UnitOfWorkEnrollmentNotificationFailed>();
    [Fact] void should_preserve_the_original_failure() => _commitError.InnerException.ShouldEqual(_notificationFailure);
    [Fact] void should_allow_rollback_to_complete_the_unit() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_discard_staged_events_on_rollback() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_append_anything() => _eventsAppended.ShouldBeNull();
}
