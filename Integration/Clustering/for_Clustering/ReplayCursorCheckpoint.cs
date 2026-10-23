// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Integration.Clustering.for_Clustering;

public class ReplayCursorCheckpoint
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _armed;
    int _entered;

    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Arm() => Volatile.Write(ref _armed, 1);

    public void Release()
    {
        Volatile.Write(ref _armed, 0);
        _release.TrySetResult();
    }

    public object? Observe(object? result) => Volatile.Read(ref _armed) == 1 && result is Task<IEventCursor> cursor
        ? Hold(cursor)
        : result;

    async Task<IEventCursor> Hold(Task<IEventCursor> operation)
    {
        var cursor = await operation;
        if (Interlocked.Increment(ref _entered) == 8) Reached.TrySetResult();
        await _release.Task;
        return cursor;
    }
}
