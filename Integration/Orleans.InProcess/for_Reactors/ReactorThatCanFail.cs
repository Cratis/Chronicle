// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors;

[DependencyInjection.IgnoreConvention]
public class ReactorThatCanFail(TaskCompletionSource tcs) : IReactor
{
    public bool ShouldFail { get; set; }
    public TimeSpan HandleTime { get; set; } = TimeSpan.Zero;
    public int HandledEvents;

    public async Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        tcs.SetResult();
        await Task.Delay(HandleTime);
        if (ShouldFail)
        {
            ShouldFail = false;
            throw new Exception("Something went wrong");
        }

        Interlocked.Increment(ref HandledEvents);
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents != count)
        {
            await Task.Delay(20, cts.Token);
        }
    }
}
