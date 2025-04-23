// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reducers;

[DependencyInjection.IgnoreConvention]
public class ReducerWithoutDelay : IReducerFor<SomeReadModel>
{
    public int HandledEvents;

    public Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        input ??= new SomeReadModel(0);
        return Task.FromResult(input with { Number = evt.Number });
    }


    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count)
        {
            await Task.Delay(20, cts.Token);
        }
    }
}
