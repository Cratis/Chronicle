// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.when_accumulating_mixed_cells;

public abstract class conformance(given.CountsHarness harness) : given.a_counts_storage(harness)
{
    ObserverHandledCountEntry[] _rows;
    int _statements;
    int _scopes;
    const string Special = "quote' ; -- 雪";

    async Task Establish()
    {
        await _harness.Storage.Increment("observer", "first", Counts(4));
        await _harness.Storage.Increment("other", "first", Counts(8));
        _harness.Reset();
    }

    async Task Because()
    {
        await _harness.Storage.Increment("observer",
        [
            new((Key)"first", Counts(2)),
            new((Key)"first", Counts(3)),
            new((Key)Special, new Dictionary<EventTypeId, EventCount> { [(EventTypeId)Special] = 7UL, ["second"] = 9UL })
        ]);
        _statements = _harness.Commands.Statements;
        _scopes = _harness.Scopes;
        _rows = await _harness.Rows();
    }

    [Fact] public void should_add_duplicate_existing_keys() => _rows.Single(_ => _.ObserverId == "observer" && _.Partition == "first").Count.ShouldEqual(9L);
    [Fact] public void should_parameterize_special_values() => _rows.Single(_ => _.Partition == Special && _.EventTypeId == Special).Count.ShouldEqual(7L);
    [Fact] public void should_preserve_other_observers() => _rows.Single(_ => _.ObserverId == "other").Count.ShouldEqual(8L);
    [Fact] public void should_preserve_all_input_deltas() => _rows.Where(_ => _.ObserverId == "observer").Sum(_ => _.Count).ShouldEqual(25L);
    [Fact] public void should_use_one_statement() => _statements.ShouldEqual(1);
    [Fact] public void should_use_one_scope() => _scopes.ShouldEqual(1);
}
