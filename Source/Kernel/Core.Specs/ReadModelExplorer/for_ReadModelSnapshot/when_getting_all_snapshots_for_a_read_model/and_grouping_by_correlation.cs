// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_grouping_by_correlation : given.a_projection_with_a_history
{
    IEnumerable<ReadModelSnapshot> _result;

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Correlation));

    [Fact] void should_return_a_snapshot_for_every_correlation() => _result.Count().ShouldEqual(2);

    [Fact] void should_gather_each_correlations_events_together() =>
        _result.Select(snapshot => snapshot.Events.Count()).ShouldEqual<IEnumerable<int>>([2, 2]);

    [Fact] void should_take_the_occurred_time_from_the_groups_first_event() =>
        _result.ElementAt(1).Occurred.ShouldEqual(DateTimeOffset.UnixEpoch.AddMinutes(3));

    [Fact] void should_name_the_correlation_each_group_belongs_to() =>
        _result.Select(snapshot => snapshot.CorrelationId).ShouldEqual<IEnumerable<Guid>>(
            [FirstCorrelation.Value, SecondCorrelation.Value]);

    [Fact] void should_fold_the_history_in_a_single_pass() => _foldCount.ShouldEqual(2);
}
