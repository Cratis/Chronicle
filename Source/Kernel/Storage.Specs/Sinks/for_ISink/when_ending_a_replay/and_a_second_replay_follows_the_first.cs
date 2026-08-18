// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

/// <summary>
/// Each replay starts from nothing, so a second one must not finish holding what the first one produced.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_a_second_replay_follows_the_first<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;

    async Task Establish()
    {
        // The read model exists before the first replay, which is the state a replay actually starts
        // from: the engine writes through the sink long before anything asks for a rewind.
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(5), 41UL);

        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.EndReplay(ReplayContext());
    }

    async Task Because()
    {
        await _sink.BeginReplay(ReplayContext());
        await _sink.EndReplay(ReplayContext());
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_not_carry_the_first_replay_into_the_second() => _count.ShouldEqual(1);
}
