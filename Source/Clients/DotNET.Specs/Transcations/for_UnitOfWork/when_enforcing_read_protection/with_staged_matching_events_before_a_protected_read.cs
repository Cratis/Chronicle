// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_staged_matching_events_before_a_protected_read : given.an_enforcing_unit
{
    void Establish() => Stage();

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddDecisionRead(_read));

    [Fact] void should_refuse_the_read_that_cannot_observe_staged_changes() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
    [Fact] void should_not_enroll_the_refused_read() => _unitOfWork.HasEnrolledDecisionReads.ShouldBeFalse();
}
