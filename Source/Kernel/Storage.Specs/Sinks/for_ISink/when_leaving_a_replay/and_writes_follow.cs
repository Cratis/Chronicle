// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_leaving_a_replay;

public abstract class and_writes_follow<THarness> : for_ISink.given.an_accumulating_read_model<THarness>
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
        await _sink.LeaveReplay();
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 44UL);
        _count = await CurrentCountOrNull();
    }

    [Fact] public void should_write_to_the_read_model_itself() => _count.ShouldEqual(3);
}
