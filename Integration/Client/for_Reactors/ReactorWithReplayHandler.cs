// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

/// <summary>
/// Counts live and replay handling separately, so a spec can tell which handler the kernel's observation
/// state actually selected once the event has been over the wire.
/// </summary>
[DependencyInjection.IgnoreConvention]
public class ReactorWithReplayHandler : IReactor
{
    public int LiveHandled;
    public int ReplayHandled;

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref LiveHandled);
        return Task.CompletedTask;
    }

    [Replay]
    public Task OnSomeEventDuringReplay(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref ReplayHandled);
        return Task.CompletedTask;
    }

    public async Task WaitTillReplayHandledReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (ReplayHandled < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }

    public async Task WaitTillLiveHandledReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (LiveHandled < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
