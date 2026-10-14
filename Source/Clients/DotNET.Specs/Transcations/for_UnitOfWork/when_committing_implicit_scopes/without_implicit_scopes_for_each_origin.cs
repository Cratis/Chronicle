// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class without_implicit_scopes_for_each_origin : given.a_unit_with_implicit_scopes
{
    readonly ConcurrencyScope _scope = new(7, EventStreamType: "items");
    void Establish()
    {
        Enroll(_scope, ConcurrencyScopeOrigin.Explicit, "explicit");
        Enroll(_scope, ConcurrencyScopeOrigin.Aggregate, "aggregate");
        Enroll(_scope, ConcurrencyScopeOrigin.Routing, "routing");
        Read(3);
    }
    Task Because() => Commit(true);
    [Fact] void should_keep_explicit_identical() => Assert.Equivalent(_scope.ToSequencesContract(), ScopeFor("explicit"));
    [Fact] void should_keep_aggregate_identical() => Assert.Equivalent(_scope.ToSequencesContract(), ScopeFor("aggregate"));
    [Fact] void should_keep_routing_identical() => Assert.Equivalent(_scope.ToSequencesContract(), ScopeFor("routing"));
    [Fact] void should_keep_decision_read_types() => ScopeFor(_sourceId).EventTypes.ShouldNotBeEmpty();
}
