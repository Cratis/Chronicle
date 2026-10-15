// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors;

[DependencyInjection.IgnoreConvention]
[StartAtTail]
public sealed class ReactorStartingAtTail : IReactor, IDisposable
{
    readonly SemaphoreSlim _handled = new(0);

    public int HandledEvents;

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        _handled.Release();
        return Task.CompletedTask;
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await _handled.WaitAsync(cts.Token);
        }
    }

    public void Dispose() => _handled.Dispose();
}
