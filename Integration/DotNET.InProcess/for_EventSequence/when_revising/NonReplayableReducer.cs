// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_revising;

[DependencyInjection.IgnoreConvention]
public class NonReplayableReducer : IReducerFor<NonReplayableReadModel>
{
    public int HandledEvents;

    public Task<NonReplayableReadModel?> OnSomeEvent(SomeEvent evt, NonReplayableReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        input ??= new NonReplayableReadModel(evt.Content, 0);
        return Task.FromResult<NonReplayableReadModel?>(input with { Content = evt.Content });
    }

    public Task<NonReplayableReadModel?> OnAnotherEvent(AnotherEvent evt, NonReplayableReadModel? input, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEvents);
        input ??= new NonReplayableReadModel(string.Empty, evt.Number);
        return Task.FromResult<NonReplayableReadModel?>(input with { Number = evt.Number });
    }

    public async Task WaitTillHandledEventReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEvents < count && !cts.IsCancellationRequested)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
