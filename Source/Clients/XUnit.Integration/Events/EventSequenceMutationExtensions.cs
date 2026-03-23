// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using KernelCorrelationId = Cratis.Execution.CorrelationId;
using KernelEventRedactionRequested = KernelCore::Cratis.Chronicle.Events.EventSequences.EventRedactionRequested;
using KernelEventRevised = KernelCore::Cratis.Chronicle.Events.EventSequences.EventRevised;
using KernelEventSequenceId = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId;
using KernelEventSequenceNumber = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber;
using KernelEventSourceId = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSourceId;
using KernelEventsRedactedForEventSource = KernelCore::Cratis.Chronicle.Events.EventSequences.EventsRedactedForEventSource;
using KernelEventType = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventType;
using KernelEventTypeGeneration = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeGeneration;
using KernelEventTypeId = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId;
using KernelIdentity = KernelConcepts::Cratis.Chronicle.Concepts.Identities.Identity;
using KernelRedactionReason = KernelConcepts::Cratis.Chronicle.Concepts.Events.RedactionReason;

namespace Cratis.Chronicle.XUnit.Integration.Events;

/// <summary>
/// Extension methods for mutating events in the event log from integration test fixtures.
/// </summary>
public static class EventSequenceMutationExtensions
{
    /// <summary>
    /// Revise a specific event in the event log with new content.
    /// </summary>
    /// <typeparam name="TEvent">Type of the event to revise with.</typeparam>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event to revise.</param>
    /// <param name="revision">The new event content to revise with.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task ReviseEvent<TEvent>(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, TEvent revision)
    {
        var eventSerializer = fixture.Services.GetRequiredService<IEventSerializer>();
        var content = await eventSerializer.Serialize(revision!);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(typeof(TEvent));
        var kernelEventType = new KernelEventType(
            new KernelEventTypeId(eventType.Id.Value),
            new KernelEventTypeGeneration(eventType.Generation.Value));

        var correlationId = KernelCorrelationId.New();
        var systemEventSequence = fixture.GetEventSequenceGrain(KernelEventSequenceId.System);
        await systemEventSequence.Append(
            new KernelEventSourceId(KernelEventSequenceId.Log.Value),
            new KernelEventRevised(
                KernelEventSequenceId.Log,
                new KernelEventSequenceNumber(sequenceNumber.Value),
                kernelEventType,
                content.ToJsonString()),
            correlationId,
            [],
            KernelIdentity.System);

        await fixture.EventLogSequenceGrain.Revise(
            new KernelEventSequenceNumber(sequenceNumber.Value),
            kernelEventType,
            content,
            correlationId,
            [],
            KernelIdentity.System);
    }

    /// <summary>
    /// Redact a specific event in the event log.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event to redact.</param>
    /// <param name="reason">Reason for the redaction.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task RedactEvent(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, string reason)
    {
        var redactionReason = new KernelRedactionReason(reason);

        var correlationId = KernelCorrelationId.New();
        var systemEventSequence = fixture.GetEventSequenceGrain(KernelEventSequenceId.System);
        await systemEventSequence.Append(
            new KernelEventSourceId(KernelEventSequenceId.Log.Value),
            new KernelEventRedactionRequested(
                KernelEventSequenceId.Log,
                new KernelEventSequenceNumber(sequenceNumber.Value),
                redactionReason),
            correlationId,
            [],
            KernelIdentity.System);

        await fixture.EventLogSequenceGrain.Redact(
            new KernelEventSequenceNumber(sequenceNumber.Value),
            redactionReason,
            correlationId,
            [],
            KernelIdentity.System);
    }

    /// <summary>
    /// Redact all events for a specific event source in the event log.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="eventSourceId">The event source to redact events for.</param>
    /// <param name="reason">Reason for the redaction.</param>
    /// <param name="eventTypes">Optional event type CLR types to restrict the redaction to.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task RedactEventsForEventSource(this IChronicleSetupFixture fixture, EventSourceId eventSourceId, string reason, params Type[] eventTypes)
    {
        var redactionReason = new KernelRedactionReason(reason);

        var kernelEventTypes = eventTypes
            .Select(fixture.EventStore.EventTypes.GetEventTypeFor)
            .Select(et => new KernelEventType(
                new KernelEventTypeId(et.Id.Value),
                new KernelEventTypeGeneration(et.Generation.Value)))
            .ToArray();

        var correlationId = KernelCorrelationId.New();
        var systemEventSequence = fixture.GetEventSequenceGrain(KernelEventSequenceId.System);
        await systemEventSequence.Append(
            new KernelEventSourceId(KernelEventSequenceId.Log.Value),
            new KernelEventsRedactedForEventSource(
                KernelEventSequenceId.Log,
                new KernelEventSourceId(eventSourceId.Value),
                kernelEventTypes,
                redactionReason),
            correlationId,
            [],
            KernelIdentity.System);

        await fixture.EventLogSequenceGrain.Redact(
            new KernelEventSourceId(eventSourceId.Value),
            redactionReason,
            kernelEventTypes,
            correlationId,
            [],
            KernelIdentity.System);
    }
}
