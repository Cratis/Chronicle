// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class after_completion_under_strict : given.a_unit_with_implicit_scopes
{
    async Task Establish()
    {
        _unit = new(CorrelationId.New(), _ => { }, _store, UnitOfWorkLifecyclePolicy.Strict);
        _enrollment = _unit;
        await _unit.Commit();
    }
    void Because() => _exception = Catch.Exception(() => Guard());
    [Fact] void should_refuse_the_late_guard() => _exception.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
}
