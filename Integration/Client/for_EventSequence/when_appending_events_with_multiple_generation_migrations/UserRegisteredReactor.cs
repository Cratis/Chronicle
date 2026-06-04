// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending_events_with_multiple_generation_migrations;

[DependencyInjection.IgnoreConvention]
public class UserRegisteredReactor : IReactor
{
    public int HandledEvents;
    public string ReceivedFirstName;
    public string ReceivedLastName;
    public uint ReceivedGeneration;

    public Task OnUserRegisteredV2(UserRegisteredV2 evt, EventContext ctx)
    {
        ReceivedFirstName = evt.FirstName;
        ReceivedLastName = evt.LastName;
        ReceivedGeneration = ctx.EventType.Generation.Value;
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
