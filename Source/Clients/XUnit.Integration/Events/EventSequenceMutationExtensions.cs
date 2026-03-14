// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;
using KernelEventType = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventType;
using KernelEventTypeId = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeId;
using KernelEventTypeGeneration = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventTypeGeneration;
using KernelEventSequenceNumber = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSequenceNumber;
using KernelEventSourceId = KernelConcepts::Cratis.Chronicle.Concepts.Events.EventSourceId;
using KernelRedactionReason = KernelConcepts::Cratis.Chronicle.Concepts.Events.RedactionReason;
using KernelCorrelationId = Cratis.Execution.CorrelationId;
using KernelIdentity = KernelConcepts::Cratis.Chronicle.Concepts.Identities.Identity;

namespace Cratis.Chronicle.XUnit.Integration.Events;

/// <summary>
/// Extension methods for mutating events in the event log from integration test fixtures.
/// </summary>
public static class EventSequenceMutationExtensions
{
    /// <summary>
    /// Compensate a specific event in the event log with new content.
    /// </summary>
    /// <typeparam name="TEvent">Type of the event to compensate with.</typeparam>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event to compensate.</param>
    /// <param name="compensation">The new event content to compensate with.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task CompensateEvent<TEvent>(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, TEvent compensation)
    {
        var eventSerializer = fixture.Services.GetRequiredService<IEventSerializer>();
        var content = await eventSerializer.Serialize(compensation!);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(typeof(TEvent));
        var kernelEventType = new KernelEventType(
            new KernelEventTypeId(eventType.Id.Value),
            new KernelEventTypeGeneration(eventType.Generation.Value));

        await fixture.EventLogSequenceGrain.Compensate(
            new KernelEventSequenceNumber(sequenceNumber.Value),
            kernelEventType,
            content,
            KernelCorrelationId.New(),
            [],
            KernelIdentity.System);
    }

    /// <summary>
    /// Redact a specific event in the event log.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event to redact.</param>
    /// <param name="reason">Optional reason for the redaction.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task RedactEvent(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, string? reason = null)
    {
        var redactionReason = reason is null
            ? KernelRedactionReason.Unknown
            : new KernelRedactionReason(reason);

        await fixture.EventLogSequenceGrain.Redact(
            new KernelEventSequenceNumber(sequenceNumber.Value),
            redactionReason,
            KernelCorrelationId.New(),
            [],
            KernelIdentity.System);
    }

    /// <summary>
    /// Redact all events for a specific event source in the event log.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="eventSourceId">The event source to redact events for.</param>
    /// <param name="reason">Optional reason for the redaction.</param>
    /// <param name="eventTypes">Optional event type CLR types to restrict the redaction to.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task RedactEventsForEventSource(this IChronicleSetupFixture fixture, EventSourceId eventSourceId, string? reason = null, params Type[] eventTypes)
    {
        var redactionReason = reason is null
            ? KernelRedactionReason.Unknown
            : new KernelRedactionReason(reason);

        var kernelEventTypes = eventTypes
            .Select(t => fixture.EventStore.EventTypes.GetEventTypeFor(t))
            .Select(et => new KernelEventType(
                new KernelEventTypeId(et.Id.Value),
                new KernelEventTypeGeneration(et.Generation.Value)))
            .ToArray();

        await fixture.EventLogSequenceGrain.Redact(
            new KernelEventSourceId(eventSourceId.Value),
            redactionReason,
            kernelEventTypes,
            KernelCorrelationId.New(),
            [],
            KernelIdentity.System);
    }
}
