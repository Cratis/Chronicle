// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

/// <summary>
/// A projection pipeline built during a replay keeps its sink after the replay ends through another sink instance.
/// Its next write belongs in the read model readers see, not in a replay container nothing will swap in again.
/// </summary>
public class and_the_replay_began_on_another_sink_for_the_read_model : for_Sink.given.two_sinks_for_one_read_model
{
    int? _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _otherSink.BeginReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
    }

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext());
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        _count = await CurrentCountOrNull();
    }

    [Fact] void should_write_what_follows_the_replay_to_the_read_model() => _count.ShouldEqual(3);
}
