// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_replacing_isolated;

public abstract class and_replay_is_active<THarness> : given.an_isolated_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    int _count;

    async Task Establish()
    {
        await Seed(10);
        await _sink.BeginReplay(ReplayContext());
    }

    async Task Because()
    {
        await _sink.ReplaceIsolated(_key, State(20), 20);
        await _sink.LeaveReplay();
        _count = await CurrentCount();
    }

    [Fact] public void should_replace_the_primary_instance() => _count.ShouldEqual(20);
}
