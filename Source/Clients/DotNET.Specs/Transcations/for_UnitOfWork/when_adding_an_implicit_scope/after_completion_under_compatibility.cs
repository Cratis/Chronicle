// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_implicit_scope;

public class after_completion_under_compatibility : given.a_unit_with_implicit_scopes
{
    Task Establish() => _unit.Commit();
    void Because() => _exception = Catch.Exception(() => Guard());
    [Fact] void should_ignore_the_late_guard() => _exception.ShouldBeNull();
    [Fact] void should_not_append() => _request.ShouldBeNull();
}
