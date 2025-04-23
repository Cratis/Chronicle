// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors;

[DependencyInjection.IgnoreConvention]
public class ReactorWithoutDelay : IReactor
{
    public int HandledEvents;

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        return Task.CompletedTask;
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents != count)
        {
            await Task.Delay(100, cts.Token);
        }
    }
}
