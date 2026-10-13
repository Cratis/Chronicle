// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_conflicting_explicit_scope : given.a_contributed_unit
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => Stage(ConcurrencyScope.None));

    [Fact] void should_reject_the_conflict() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_not_stage_any_event() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_notify_the_contributor() => _contributor.DidNotReceive().Enrolled(Arg.Any<UnitOfWorkEnrollment>(), Arg.Any<ConcurrencyScope>());
}
