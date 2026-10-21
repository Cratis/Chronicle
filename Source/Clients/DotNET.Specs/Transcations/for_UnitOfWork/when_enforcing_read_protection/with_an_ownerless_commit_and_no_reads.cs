// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_an_ownerless_commit_and_no_reads : given.an_enforcing_unit
{
    async Task Because() => _error = await Catch.Exception(_unitOfWork.Commit);

    [Fact] void should_require_the_owner_even_without_read_dependencies() => _error.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
}
