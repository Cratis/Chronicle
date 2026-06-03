// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[DependencyInjection.IgnoreConvention]
public class SomeReactor : IReactor
{
    public int HandledEvents;

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        return Task.CompletedTask;
    }

    public Task OnAnotherEvent(AnotherEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        return Task.CompletedTask;
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
