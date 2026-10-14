// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_late_staging_after_owner_completion : given.an_enforcing_unit
{
    async Task Establish() => await _unitOfWork.CommitAsOwner(_owner);

    void Because() => _error = Catch.Exception(() => Stage());

    [Fact] void should_make_enforce_strict_without_decision_reads() => _error.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
}
