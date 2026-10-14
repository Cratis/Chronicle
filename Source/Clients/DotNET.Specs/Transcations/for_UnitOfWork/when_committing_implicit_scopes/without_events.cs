// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class without_events : given.a_unit_with_implicit_scopes
{
    void Establish() => Guard();
    Task Because() => Commit();
    [Fact] void should_complete_without_binding_or_appending() => _request.ShouldBeNull();
    [Fact] void should_complete() => _unit.IsCompleted.ShouldBeTrue();
}
