// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[DependencyInjection.IgnoreConvention]
public class ReactorSubscribedToEventRedacted : IReactor
{
    public int HandledSomeEvents;
    public int HandledEventRedactedEvents;
    public List<Type> RedactedOriginalTypes = [];
    readonly object _redactedOriginalTypesLock = new();

    public Task OnSomeEvent(SomeEvent evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledSomeEvents);
        return Task.CompletedTask;
    }

    public Task OnEventRedacted(EventRedacted evt, EventContext ctx)
    {
        Interlocked.Increment(ref HandledEventRedactedEvents);
        lock (_redactedOriginalTypesLock)
        {
            RedactedOriginalTypes.Add(evt.OriginalEventType);
        }
        return Task.CompletedTask;
    }

    public async Task WaitTillEventRedactedHandledReaches(int count, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (HandledEventRedactedEvents < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
