// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Integration.for_Reactors.when_replaying;

[EventSequence("inbox-reactor-replay-source")]
[DependencyInjection.IgnoreConvention]
public class InboxReplayReactor : IReactor
{
    public const string SourceEventStore = "reactor-replay-source";
    public int LiveHandled;
    public int ReplayHandled;
    public EventObservationState ReplayObservationState;

    public Task On(InboxReplayEvent @event, EventContext context)
    {
        Interlocked.Increment(ref LiveHandled);
        return Task.CompletedTask;
    }

    [Replay]
    public Task OnReplay(InboxReplayEvent @event, EventContext context)
    {
        ReplayObservationState = context.ObservationState;
        Interlocked.Increment(ref ReplayHandled);
        return Task.CompletedTask;
    }

    public async Task WaitTillHandled(int count, bool replay = false)
    {
        using var cts = new CancellationTokenSource(TimeSpanFactory.DefaultTimeout());
        while ((replay ? Volatile.Read(ref ReplayHandled) : Volatile.Read(ref LiveHandled)) < count)
        {
            await Task.Delay(50, cts.Token);
        }
    }
}
