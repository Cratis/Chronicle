// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_crossing_statement_boundaries;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    int _firstStatements;
    ObserverHandledCountEntry[] _rows;

    async Task Because()
    {
        await _harness.Storage.Increment("first", Cells(181));
        _firstStatements = _harness.Commands.Statements;
        _harness.Reset();
        await _harness.Storage.Increment("second", Cells(361));
        _rows = await _harness.Rows();
    }

    [Fact] public void should_split_181_cells_into_two_statements() => _firstStatements.ShouldEqual(2);
    [Fact] public void should_split_361_cells_into_three_statements() => _harness.Commands.Statements.ShouldEqual(3);
    [Fact] public void should_use_one_scope_for_361_cells() => _harness.Scopes.ShouldEqual(1);
    [Fact] public void should_commit_once_for_361_cells() => _harness.Commit.Attempts.ShouldEqual(1);
    [Fact] public void should_bound_parameters() => _harness.Commands.LargestParameterCount.ShouldBeLessThanOrEqual(900);
    [Fact] public void should_store_all_181_cells() => _rows.Count(_ => _.ObserverId == "first" && _.Count == 1).ShouldEqual(181);
    [Fact] public void should_store_all_361_cells() => _rows.Count(_ => _.ObserverId == "second" && _.Count == 1).ShouldEqual(361);
}
