// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_input_is_empty;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    async Task Because()
    {
        await _harness.Storage.Increment("observer", []);
        await _harness.Storage.Increment("observer", "partition", new Dictionary<EventTypeId, EventCount>());
        await _harness.Storage.Increment("observer", [new((Key)"partition", new Dictionary<EventTypeId, EventCount>())]);
    }

    [Fact] public void should_not_open_a_namespace_scope() => _harness.Scopes.ShouldEqual(0);
    [Fact] public void should_not_execute_count_statements() => _harness.Commands.Statements.ShouldEqual(0);
    [Fact] public void should_not_commit_a_transaction() => _harness.Commit.Attempts.ShouldEqual(0);
}
