// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_applying_changes_during_a_replay;

/// <summary>
/// A projection pipeline built before a replay began keeps its sink while the replay runs through another sink
/// instance. What it writes during the replay belongs in the replay container that the end of the replay swaps in.
/// </summary>
public class and_the_replay_began_on_another_sink_for_the_read_model : for_Sink.given.two_sinks_for_one_read_model
{
    int? _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(new Key("counter-2", ArrayIndexers.NoIndexers), ChangesetSettingCountTo(5), 42UL);
    }

    async Task Because()
    {
        await _otherSink.ApplyChanges(_key, ChangesetSettingCountTo(2), 43UL);
        await _sink.EndReplay(ReplayContext());
        _count = await CurrentCountOrNull();
    }

    [Fact] void should_keep_what_it_wrote_once_the_replay_ends() => _count.ShouldEqual(2);
}
