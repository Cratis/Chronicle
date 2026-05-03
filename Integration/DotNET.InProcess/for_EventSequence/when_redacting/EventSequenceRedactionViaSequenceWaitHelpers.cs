// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelGlobalEventTypes = Cratis.Chronicle.Concepts.Events.GlobalEventTypes;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting;

static class EventSequenceRedactionViaSequenceWaitHelpers
{
    public static async Task<KernelAppendedEvent> WaitForRedactedEvent(this IChronicleSetupFixture fixture, Events.EventSequenceNumber sequenceNumber, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);
        var storage = fixture.GetEventLogStorage();

        while (true)
        {
            var storedEvent = await storage.GetEventAt(sequenceNumber.Value);
            if (storedEvent.Context.EventType.Id == KernelGlobalEventTypes.Redaction)
            {
                return storedEvent;
            }

            await Task.Delay(50, cts.Token);
        }
    }

    public static async Task<KernelAppendedEvent> WaitForSystemEvent(this IChronicleSetupFixture fixture, string eventTypeId, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.FromSeconds(30);
        using var cts = new CancellationTokenSource(timeout.Value);
        var systemStorage = fixture.GetSystemEventLogStorage();

        while (true)
        {
            var tailSequenceNumber = await systemStorage.GetTailSequenceNumber();
            var storedEvent = await systemStorage.GetEventAt(tailSequenceNumber);
            if (storedEvent.Context.EventType.Id.Value == eventTypeId)
            {
                return storedEvent;
            }

            await Task.Delay(50, cts.Token);
        }
    }
}
