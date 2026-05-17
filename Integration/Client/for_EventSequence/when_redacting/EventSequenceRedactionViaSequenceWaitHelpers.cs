// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

static class EventSequenceRedactionViaSequenceWaitHelpers
{
    public static async Task<AppendedEvent> WaitForRedactedEvent(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);

        while (true)
        {
            var events = await fixture.EventStore.EventLog.GetFromSequenceNumber(sequenceNumber);
            var storedEvent = events.FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber);

            if (storedEvent is not null && storedEvent.Context.EventType.Id.Value == "EventRedacted")
            {
                return storedEvent;
            }

            await Task.Delay(50, cts.Token);
        }
    }

    public static async Task<AppendedEvent> WaitForSystemEvent(this IChronicleSetupFixture fixture, string eventTypeId, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);

        var systemLog = fixture.EventStore.GetEventSequence(EventSequenceId.System);

        while (true)
        {
            var tail = await systemLog.GetTailSequenceNumber();
            if (tail != EventSequenceNumber.Unavailable)
            {
                var events = await systemLog.GetFromSequenceNumber(tail);
                var storedEvent = events.FirstOrDefault();

                if (storedEvent?.Context.EventType.Id.Value == eventTypeId)
                {
                    return storedEvent;
                }
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
