// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_definition_routing_on_the_label : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard();
        _enrollment.AddEvents(EventSequenceId.Log, [Item("one")], []);
    }
    Task Because() => Commit();
    [Fact] void should_resolve_routing_then_send_the_source_wide_guard() => Assert.Equivalent(new ConcurrencyScope(5, _sourceId).ToSequencesContract(), ScopeFor(_sourceId));
}
