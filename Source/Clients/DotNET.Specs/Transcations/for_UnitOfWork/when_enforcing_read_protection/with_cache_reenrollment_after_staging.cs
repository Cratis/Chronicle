// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_cache_reenrollment_after_staging : given.an_enforcing_unit
{
    void Establish()
    {
        _unitOfWork.AddDecisionRead(_read);
        Stage();
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddDecisionRead(_read));

    [Fact] void should_recheck_overlap_even_for_the_identical_cached_token() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
}
