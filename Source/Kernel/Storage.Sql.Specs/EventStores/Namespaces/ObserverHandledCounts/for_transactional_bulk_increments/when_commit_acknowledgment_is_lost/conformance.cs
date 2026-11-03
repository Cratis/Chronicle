// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_commit_acknowledgment_is_lost;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    Exception _error;
    ObserverHandledCountEntry[] _rows;

    void Establish() => _harness.Commit.Enabled = true;

    async Task Because()
    {
        _error = await Catch.Exception(() => _harness.Storage.Increment("observer", "partition", Counts(3)));
        _rows = await _harness.Rows();
    }

    [Fact] public void should_surface_the_unknown_outcome() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] public void should_not_replay_the_transaction() => _harness.Commit.Attempts.ShouldEqual(1);
    [Fact] public void should_have_executed_one_additive_statement() => _harness.Commands.Statements.ShouldEqual(1);
    [Fact] public void should_show_the_real_commit_in_a_fresh_context() => _rows.Single().Count.ShouldEqual(3L);
}
