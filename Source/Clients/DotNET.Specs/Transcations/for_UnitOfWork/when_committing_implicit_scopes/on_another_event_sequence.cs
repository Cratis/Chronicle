// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class on_another_event_sequence : given.a_unit_with_implicit_scopes
{
    void Establish()
    {
        Guard(sequence: "other-sequence");
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_commit() => _exception.ShouldBeNull();
    [Fact] void should_keep_the_resolved_scope_unchanged() => Assert.Equivalent(new ConcurrencyScope(10, _sourceId, EventStreamType.All, EventStreamId.Default, EventSourceType.Default).ToSequencesContract(), ScopeFor(_sourceId));
}
