// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Cratis.Chronicle.EventStores.for_EventStoreNames.when_observing_event_stores.given;

/// <summary>
/// A scheduler whose callbacks have already started by the time anyone cancels them, so cancelling never stops them.
/// </summary>
public class started_retry_scheduler : IScheduler
{
    readonly List<Action> _started = [];

    public DateTimeOffset Now => DateTimeOffset.UnixEpoch;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action) => Capture(state, action);

    public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action) => Capture(state, action);

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action) => Capture(state, action);

    public void FinishStartedCallbacks()
    {
        var started = _started.ToArray();
        _started.Clear();
        foreach (var callback in started)
        {
            callback();
        }
    }

    IDisposable Capture<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        _started.Add(() => action(this, state));
        return Disposable.Empty;
    }
}
