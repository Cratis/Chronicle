// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_a_cross_tenant_contribution : given.an_enforcing_unit
{
    void Establish() => _contribution = _contribution with { Namespace = "other-tenant" };

    void Because() => _error = Catch.Exception(() => _unitOfWork.EnrolContributedRead(_contribution));

    [Fact] void should_check_the_full_target_before_the_no_read_rule() => _error.ShouldBeOfExactType<ReadDependencyTargetMismatch>();
}
