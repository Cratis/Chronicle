// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_the_second_statement_fails;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    Exception _error;
    ObserverHandledCountEntry[] _afterFailure;
    ObserverHandledCountEntry[] _afterRetry;
    int _attempts;

    async Task Establish()
    {
        await _harness.Storage.Increment("observer", "partition-0000", Counts(10));
        await _harness.Storage.Increment("earlier-call", "independent", Counts(7));
        _harness.Reset();
        _harness.Commands.FailAt = 2;
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _harness.Storage.Increment("observer", Cells(361)));
        _attempts = _harness.Commands.Statements;
        _afterFailure = await _harness.Rows();
        _harness.Reset();
        await _harness.Storage.Increment("observer", Cells(361));
        _afterRetry = await _harness.Rows();
    }

    [Fact] public void should_propagate_the_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] public void should_stop_at_the_second_statement() => _attempts.ShouldEqual(2);
    [Fact] public void should_not_leave_inserted_rows() => _afterFailure.Length.ShouldEqual(2);
    [Fact] public void should_roll_back_existing_row_updates() => _afterFailure.Single(_ => _.ObserverId == "observer").Count.ShouldEqual(10L);
    [Fact] public void should_preserve_the_earlier_committed_call() => _afterFailure.Single(_ => _.ObserverId == "earlier-call").Count.ShouldEqual(7L);
    [Fact] public void should_apply_the_known_rollback_retry_exactly() => _afterRetry.Where(_ => _.ObserverId == "observer").Sum(_ => _.Count).ShouldEqual(371L);
    [Fact] public void should_insert_all_cells_on_retry() => _afterRetry.Count(_ => _.ObserverId == "observer").ShouldEqual(361);
}
