// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_the_contributor_reenters : given.a_contributed_unit
{
    Exception _error;

    void Establish() => _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_ =>
    {
        Stage();
        return _required;
    });

    void Because() => _error = Catch.Exception(() => Stage());

    [Fact] void should_refuse_reentrant_enrollment() => _error.ShouldBeOfExactType<UnitOfWorkMutatedDuringEnrollment>();
    [Fact] void should_stage_nothing() => _unitOfWork.GetEvents().ShouldBeEmpty();
}
