// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_enrollment_after_owner_completion : given.an_enforcing_unit
{
    async Task Establish() => await _unitOfWork.CommitAsOwner(_owner);

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddDecisionRead(_read));

    [Fact] void should_refuse_reads_after_early_completion() => _error.ShouldBeOfExactType<ReadAfterProtectedCommit>();
}
