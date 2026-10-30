// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_observing_instances_during_a_replay;

public class and_the_initial_page_is_read_between_the_table_renames : when_ending_a_replay.given.a_replay_with_an_interleaved_reader
{
    readonly ObservedCounts _observed = new();
    int[]? _firstPage;
    int[]? _rebuiltPage;
    int[]? _pageAfterTheReplay;

    void Establish() => _afterPrimaryRename = async () =>
    {
        _observed.Observe(_reader);
        _firstPage = await _observed.PageWhere(_ => true);
    };

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext());
        _rebuiltPage = await _observed.PageWhere(counts => counts.SequenceEqual([2]));
        await _reader.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        _pageAfterTheReplay = await _observed.PageWhere(counts => counts.SequenceEqual([3]));
    }

    void Destroy() => _observed.Dispose();

    [Fact] void should_stream_the_previously_committed_state_during_promotion() => _firstPage.ShouldContainOnly(1);
    [Fact] void should_stream_the_rebuilt_state_after_promotion() => _rebuiltPage.ShouldContainOnly(2);
    [Fact] void should_stream_a_write_to_the_promoted_read_model() => _pageAfterTheReplay.ShouldContainOnly(3);
}
