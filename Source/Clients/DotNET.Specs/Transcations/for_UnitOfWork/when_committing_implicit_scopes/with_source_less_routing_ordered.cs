// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_source_less_routing_ordered : given.a_unit_with_implicit_scopes
{
    readonly ConcurrencyScope _routing = new(1, EventStreamType: "items");
    protected override bool Ordered => true;
    void Establish()
    {
        Guard();
        Enroll(_routing, ConcurrencyScopeOrigin.Routing);
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_send_the_implicit_guard() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
    [Fact] void should_preserve_routing_independently() => Assert.Equivalent(_routing.ToSequencesContract(), ScopeFor($"$keyguard:routing:{_sourceId.Value}"));
}
