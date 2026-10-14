// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_decision_read_without_owner : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Read(3);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_still_require_owner_completion() => _exception.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
}
