// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_ending_a_replay;

public class and_a_second_replay_follows_the_first : given.a_sink_with_a_replayed_read_model
{
    int? _count;

    async Task Establish()
    {
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.EndReplay(ReplayContext());
    }

    async Task Because()
    {
        await _sink.BeginReplay(ReplayContext());
        await _sink.EndReplay(ReplayContext());
        _count = await CurrentCount();
    }

    [Fact] void should_not_carry_the_first_replay_into_the_second() => _count.ShouldEqual(1);
}
