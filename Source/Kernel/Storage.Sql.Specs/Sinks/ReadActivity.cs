// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Tracks the table scopes a sink has open, so a specification can wait for polls still in flight after it unsubscribed.
/// </summary>
public sealed class ReadActivity
{
    int _active;
    TaskCompletionSource _changed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Marks a read as started.
    /// </summary>
    /// <returns>The action that marks it finished; calling it more than once has no further effect.</returns>
    public Action Begin()
    {
        Interlocked.Increment(ref _active);
        SignalChange();
        var finished = 0;
        return () =>
        {
            if (Interlocked.Exchange(ref finished, 1) == 0)
            {
                Interlocked.Decrement(ref _active);
                SignalChange();
            }
        };
    }

    /// <summary>
    /// Waits until no read is in flight and none has started for a settling period, which is longer than the polling interval.
    /// </summary>
    /// <param name="settle">How long no read may start or be in flight.</param>
    /// <param name="deadline">The longest time to wait.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task WhenQuiet(TimeSpan settle, TimeSpan deadline)
    {
        using var timeout = new CancellationTokenSource(deadline);
        while (true)
        {
            // Take the change signal before reading the state, so a read that starts or finishes afterwards completes it.
            var changed = Volatile.Read(ref _changed).Task;
            if (Volatile.Read(ref _active) != 0)
            {
                await changed.WaitAsync(timeout.Token);
                continue;
            }

            // Nothing is in flight: it is quiet once the settling period passes without any read starting or finishing.
            using var settling = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            settling.CancelAfter(settle);
            var settled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = settling.Token.Register(() => settled.TrySetResult());
            var first = await Task.WhenAny(changed, settled.Task);
            timeout.Token.ThrowIfCancellationRequested();
            if (first == settled.Task)
            {
                return;
            }
        }
    }

    void SignalChange() =>
        Interlocked.Exchange(ref _changed, new(TaskCreationOptions.RunContinuationsAsynchronously)).TrySetResult();
}
