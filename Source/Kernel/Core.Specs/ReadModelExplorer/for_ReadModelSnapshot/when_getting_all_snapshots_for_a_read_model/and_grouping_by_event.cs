// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_grouping_by_event : given.a_projection_with_a_history
{
    IEnumerable<ReadModelSnapshot> _result;

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Event));

    [Fact] void should_return_a_snapshot_for_every_event() => _result.Count().ShouldEqual(4);

    [Fact] void should_carry_one_event_in_every_snapshot() =>
        _result.Select(snapshot => snapshot.Events.Count()).Distinct().ShouldEqual<IEnumerable<int>>([1]);

    [Fact] void should_keep_the_events_in_order() =>
        _result.Select(snapshot => snapshot.Events.First().Context.SequenceNumber).ShouldEqual<IEnumerable<ulong>>([1, 2, 3, 4]);

    [Fact] void should_take_the_occurred_time_from_the_event() =>
        _result.ElementAt(2).Occurred.ShouldEqual(DateTimeOffset.UnixEpoch.AddMinutes(3));

    [Fact] void should_still_say_which_correlation_each_event_was_appended_under() =>
        _result.Select(snapshot => snapshot.CorrelationId).ShouldEqual<IEnumerable<Guid>>(
            [FirstCorrelation.Value, FirstCorrelation.Value, SecondCorrelation.Value, SecondCorrelation.Value]);

    [Fact] void should_fold_the_history_in_a_single_pass() => _foldCount.ShouldEqual(4);
}
