// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_an_event_with_origin;

public class implicit_origin_ordered : given.a_unit_with_implicit_scopes
{
    protected override bool Ordered => true;
    void Because() => _exception = Catch.Exception(() => Enroll(new(1, _sourceId), ConcurrencyScopeOrigin.Implicit));
    [Fact] void should_reject_the_reserved_origin() => _exception.ShouldBeOfExactType<ConcurrencyScopeOriginNotValidForEvents>();
    [Fact] void should_not_stage_events() => _unit.GetEvents().ShouldBeEmpty();
}
