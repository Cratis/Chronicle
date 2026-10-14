// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_a_bound_decision_read_without_events : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        Read(3);
    }
    Task Because() => Commit(true);
    [Fact] void should_keep_validate_only() => _request.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_typed_scope_unchanged() => Assert.Equivalent(new ConcurrencyScope(3, _sourceId, EventTypes: [new EventType("read", EventTypeGeneration.First)]).ToSequencesContract(), ScopeFor(_sourceId));
}
