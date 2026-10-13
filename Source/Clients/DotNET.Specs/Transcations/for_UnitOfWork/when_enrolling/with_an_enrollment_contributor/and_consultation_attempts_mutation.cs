// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_consultation_attempts_mutation : given.a_mutation_probe
{
    void Establish() => _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_ =>
    {
        ProbeMutations();
        return _required;
    });

    void Because() => Stage();

    [Fact] void should_reject_every_mutation_path() => _mutationErrors.Select(_ => _?.GetType()).ShouldEqual(Enumerable.Repeat(typeof(UnitOfWorkMutatedDuringEnrollment), 10));
    [Fact] void should_still_stage_the_outer_enrollment() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
