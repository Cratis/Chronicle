// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_ending_a_replay;

/// <summary>
/// A replay that observed no keys is a no-op, not an instruction to empty the read model - promoting an
/// empty replay container would turn a transient race into permanent data loss.
/// </summary>
/// <typeparam name="THarness">The <see cref="ISinkHarness"/> supplying the implementation under specification.</typeparam>
public abstract class and_the_replay_produced_no_writes<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int? _count;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 42UL);
        await _sink.BeginReplay(ReplayContext());
    }

    async Task Because()
    {
        await _sink.EndReplay(ReplayContext());
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_leave_the_read_model_alone() => _count.ShouldEqual(1);
}
