// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_overlap_across_different_labels : given.an_enforcing_unit
{
    void Establish()
    {
        Stage();
        _contribution = _contribution with { Label = "reservation-check" };
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_check_the_actual_source_predicate_instead_of_the_label() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
}
