// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_distinct_aggregate_contributions : given.an_enforcing_unit
{
    void Establish()
    {
        _contribution = _contribution with { Origin = ConcurrencyScopeOrigin.Aggregate };
        _unitOfWork.EnrolContributedRead(_contribution);
        _contribution = _contribution with { Scope = _contribution.Scope with { SequenceNumber = 101 }, Description = "another aggregate" };
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_refuse_instead_of_overwriting_the_first_aggregate() => _error.ShouldBeOfExactType<ConflictingContributionsOnLabel>();
}
