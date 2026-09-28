// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay;

/// <summary>
/// A subscription that starts during a replay outlives it. Once the replay is promoted, it follows the read model the
/// replay produced and sees what is written to it afterwards.
/// </summary>
public class and_the_replay_ends_while_observing : given.an_observed_read_model_being_replayed
{
    int[]? _firstPage;
    int[]? _pageAfterTheReplay;
    long _totalCount;

    async Task Because()
    {
        Observe();
        _firstPage = await PageWhere(_ => true);
        await _otherSink.EndReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(3), 44UL);
        _pageAfterTheReplay = await PageWhere(counts => counts.Contains(3));
        _totalCount = (await _sink.GetInstances()).TotalCount;
    }

    [Fact] void should_first_stream_the_state_before_the_replay() => _firstPage.ShouldContainOnly(1);
    [Fact] void should_stream_a_write_to_the_promoted_read_model() => _pageAfterTheReplay.ShouldContainOnly(3, 5);
    [Fact] void should_count_the_instances_it_streams() => _totalCount.ShouldEqual(_pageAfterTheReplay?.Length ?? 0);
}
