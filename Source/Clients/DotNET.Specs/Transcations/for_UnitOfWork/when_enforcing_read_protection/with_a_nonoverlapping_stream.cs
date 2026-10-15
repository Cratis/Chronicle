// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_a_nonoverlapping_stream : given.an_enforcing_unit
{
    void Establish()
    {
        Stage(stream: "Approval");
        _contribution = _contribution with { Scope = _contribution.Scope with { EventStreamType = "Transactions" } };
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_allow_reads_that_do_not_overlap_the_staged_predicate() => _error.ShouldBeNull();
}
