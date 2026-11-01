// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_lazily_indexing_bulk_state.given;

public class a_paused_first_lookup : a_bulk_sink_with_cached_children
{
    protected Task<Key?> _lookup;
    protected readonly TaskCompletionSource _scanStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected readonly TaskCompletionSource _releaseScan = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    async Task Establish()
    {
        await Apply(_first, WithChildren(new PausedChildren(Child("old"), _scanStarted, _releaseScan)));
        _lookup = Task.Run(() => Find("old"));
        await _scanStarted.Task.WaitAsync(Deadline);
    }

    async Task Destroy()
    {
        _releaseScan.TrySetResult();
        await _lookup.WaitAsync(Deadline);
    }

    sealed class PausedChildren(object child, TaskCompletionSource started, TaskCompletionSource release) : IEnumerable<object>
    {
        int _enumerations;

        public IEnumerator<object> GetEnumerator()
        {
            if (Interlocked.Increment(ref _enumerations) == 1)
            {
                started.TrySetResult();
                release.Task.WaitAsync(Deadline).GetAwaiter().GetResult();
            }

            yield return child;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
