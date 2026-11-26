// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

static class EventSequenceRedactionViaSequenceWaitHelpers
{
    public static async Task<AppendedEvent> WaitForRedactedEvent(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, TimeSpan? timeout = default)
    {
        return (await ReadinessWait.Until(
            async () => (await fixture.EventStore.EventLog.GetFromSequenceNumber(sequenceNumber))
                .FirstOrDefault(_ => _.Context.SequenceNumber == sequenceNumber),
            storedEvent => storedEvent?.Context.EventType.Id.Value == "EventRedacted",
            timeout ?? TimeSpanFactory.FromSeconds(30)))!;
    }

    public static async Task<AppendedEvent> WaitForSystemEvent(this IChronicleSetupFixture fixture, string eventTypeId, TimeSpan? timeout = default)
    {
        var systemLog = fixture.EventStore.GetEventSequence(EventSequenceId.System);
        return (await ReadinessWait.Until(
            async () =>
            {
                var tail = await systemLog.GetTailSequenceNumber();
                if (tail == EventSequenceNumber.Unavailable)
                {
                    return null;
                }

                var events = await systemLog.GetFromSequenceNumber(tail);
                return events.Count > 0 ? events[0] : null;
            },
            storedEvent => storedEvent?.Context.EventType.Id.Value == eventTypeId,
            timeout ?? TimeSpanFactory.FromSeconds(30)))!;
    }
}
