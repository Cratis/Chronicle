// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tasks;

/// <summary>
/// Represents an async implementation of <see cref="ManualResetEventSlim"/> that is more suited to working in a modern .net environment.
/// https://devblogs.microsoft.com/pfxteam/building-async-coordination-primitives-part-1-asyncmanualresetevent/.
/// </summary>
[DependencyInjection.IgnoreConvention]
public sealed class AsyncManualResetEvent
{
    volatile TaskCompletionSource<bool> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Waits asynchronously for the reset.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public Task WaitAsync() => _tcs.Task;

    /// <summary>
    /// Sets the reset event.
    /// </summary>
    public void Set() => _tcs.TrySetResult(true);

    /// <summary>
    /// Resets the event.
    /// </summary>
    public void Reset()
    {
        while (true)
        {
            var tcs = _tcs;
            if (!tcs.Task.IsCompleted ||
                Interlocked.CompareExchange(ref _tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
            {
                return;
            }
        }
    }
}
