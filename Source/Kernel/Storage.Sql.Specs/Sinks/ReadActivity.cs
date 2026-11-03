// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// Tracks the table scopes a sink has open, so a specification can wait for polls still in flight after it unsubscribed.
/// </summary>
public sealed class ReadActivity
{
    int _active;
    int _started;

    /// <summary>
    /// Marks a read as started.
    /// </summary>
    /// <returns>The action that marks it finished; calling it more than once has no further effect.</returns>
    public Action Begin()
    {
        Interlocked.Increment(ref _started);
        Interlocked.Increment(ref _active);
        var finished = 0;
        return () =>
        {
            if (Interlocked.Exchange(ref finished, 1) == 0)
            {
                Interlocked.Decrement(ref _active);
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
        var quietSince = Environment.TickCount64;
        var started = Volatile.Read(ref _started);
        while (true)
        {
            await Task.Delay(10, timeout.Token);
            var current = Volatile.Read(ref _started);
            if (Volatile.Read(ref _active) != 0 || current != started)
            {
                started = current;
                quietSince = Environment.TickCount64;
            }
            else if (Environment.TickCount64 - quietSince >= settle.TotalMilliseconds)
            {
                return;
            }
        }
    }
}
