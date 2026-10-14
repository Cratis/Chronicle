// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_routing_on_the_label : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Enroll(new(1, _sourceId, EventStreamId: "stream"), ConcurrencyScopeOrigin.Routing);
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_replace_routing_with_the_global_boundary() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
