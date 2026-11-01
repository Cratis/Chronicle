// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state;

public class and_reset_arrives_during_the_first_scan : given.a_paused_first_lookup
{
    object? _previous;
    object? _reset;
    Key? _old;
    Key? _current;

    async Task Because()
    {
        _previous = ChildIndex;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reset = Task.Run(async () =>
        {
            started.TrySetResult();
            await _sink.BeginBulk();
        });
        try
        {
            await started.Task.WaitAsync(Deadline);
        }
        finally
        {
            _releaseScan.TrySetResult();
        }

        await Task.WhenAll(_lookup, reset).WaitAsync(Deadline);
        _reset = ChildIndex;
        await Apply(_first, Root("current"));
        _current = await Find("current");
        _old = await Find("old");
    }

    [Fact] void should_return_to_inactive() => _reset.ShouldBeNull();
    [Fact] void should_detach_the_previous_generation() => ReferenceEquals(ChildIndex, _previous).ShouldBeFalse();
    [Fact] void should_find_the_new_generation() => _current.ShouldEqual(_first);
    [Fact] void should_not_reuse_the_old_generation() => _old.ShouldBeNull();
}
