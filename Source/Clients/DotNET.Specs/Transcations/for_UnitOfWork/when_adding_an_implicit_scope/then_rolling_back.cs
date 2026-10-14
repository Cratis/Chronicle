// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class then_rolling_back : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Aggregate);
    }
    Task Because() => _unit.Rollback();
    [Fact] void should_clear_staged_events() => _unit.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_append() => _request.ShouldBeNull();
}
