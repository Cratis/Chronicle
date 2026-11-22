// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

[DependencyInjection.IgnoreConvention]
public class SomeReducer : IReducerFor<SomeReadModel>
{
    public int HandledEvents;

    public Task<SomeReadModel?> OnSomeEvent(SomeEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        input ??= new SomeReadModel(evt.Content, 0);
        return Task.FromResult<SomeReadModel?>(input with { Content = evt.Content });
    }

    public Task<SomeReadModel?> OnAnotherEvent(AnotherEvent evt, SomeReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        input ??= new SomeReadModel(string.Empty, evt.Number);
        return Task.FromResult<SomeReadModel?>(input with { Number = evt.Number });
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
