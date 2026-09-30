// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_ReadModelChangeStreams.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose timers fire straight away, so a spec exercising the retry backoff observes
/// the retries without waiting out the real delays.
/// </summary>
public sealed class an_immediate_time_provider : TimeProvider
{
    /// <inheritdoc/>
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) =>
        new immediate_timer(callback, state);

    sealed class immediate_timer : ITimer
    {
        public immediate_timer(TimerCallback callback, object? state) =>
            ThreadPool.QueueUserWorkItem(_ => callback(state));

        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
