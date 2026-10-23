// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors.when_replaying;

[EventSequence("inbox-reactor-replay-source")]
[DependencyInjection.IgnoreConvention]
public class InboxReplayReactor : IReactor
{
    public const string SourceEventStore = "reactor-replay-source";
    public int LiveHandled;
    public int ReplayHandled;
    public EventObservationState ReplayObservationState;
    readonly object _lock = new();
    readonly List<(int Count, bool Replay, TaskCompletionSource Signal)> _waiters = [];

    public Task On(InboxReplayEvent @event, EventContext context)
    {
        Interlocked.Increment(ref LiveHandled);
        Notify();
        return Task.CompletedTask;
    }

    [Replay]
    public Task OnReplay(InboxReplayEvent @event, EventContext context)
    {
        ReplayObservationState = context.ObservationState;
        Interlocked.Increment(ref ReplayHandled);
        Notify();
        return Task.CompletedTask;
    }

    public Task WaitTillHandled(int count, bool replay = false)
    {
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            if (Handled(replay) >= count)
            {
                return Task.CompletedTask;
            }

            _waiters.Add((count, replay, signal));
        }

        return signal.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
    }

    int Handled(bool replay) => replay ? Volatile.Read(ref ReplayHandled) : Volatile.Read(ref LiveHandled);

    void Notify()
    {
        lock (_lock)
        {
            foreach (var waiter in _waiters.Where(_ => Handled(_.Replay) >= _.Count).ToArray())
            {
                _waiters.Remove(waiter);
                waiter.Signal.TrySetResult();
            }
        }
    }
}
