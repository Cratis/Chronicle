// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;
using System.Diagnostics;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using KernelAppendResult = KernelCore::Cratis.Chronicle.EventSequences.AppendResult;
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
        // Off the event store, never the root provider: IEventSerializer is scoped, so resolving it from the
        // root either throws under ValidateScopes or hands back a serializer bound to a different event store's
        // event types than the one being mutated here.
        var eventSerializer = ((EventStore)fixture.EventStore).EventSerializer;
        var content = await eventSerializer.Serialize(revision!);
        var eventType = fixture.EventStore.EventTypes.GetEventTypeFor(typeof(TEvent));
        var kernelEventType = new KernelEventType(
            new KernelEventTypeId(eventType.Id.Value),
            new KernelEventTypeGeneration(eventType.Generation.Value));

        await AppendAndWait(
            fixture,
            new KernelEventRevised(
                KernelEventSequenceId.Log,
                new KernelEventSequenceNumber(sequenceNumber.Value),
                kernelEventType,
                content.ToJsonString()),
            $"Log/{sequenceNumber.Value}");
    }

    /// <summary>
    /// Redact a specific event in the event log.
    /// </summary>
    /// <param name="fixture">The fixture to use.</param>
    /// <param name="sequenceNumber">The sequence number of the event to redact.</param>
    /// <param name="reason">Reason for the redaction.</param>
    /// <returns>Awaitable task.</returns>
    public static Task RedactEvent(this IChronicleSetupFixture fixture, EventSequenceNumber sequenceNumber, string reason) =>
        AppendAndWait(
            fixture,
            new KernelEventRedactionRequested(
                KernelEventSequenceId.Log,
                new KernelEventSequenceNumber(sequenceNumber.Value),
                new KernelRedactionReason(reason)),
            $"Log/{sequenceNumber.Value}");

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

        await AppendAndWait(
            fixture,
            new KernelEventsRedactedForEventSource(
                KernelEventSequenceId.Log,
                new KernelEventSourceId(eventSourceId.Value),
                kernelEventTypes,
                redactionReason),
            $"Log/source {eventSourceId.Value}");
    }

    /// <summary>
    /// Append a mutation request to the System event sequence and wait until the event sequences reactor has handled it.
    /// </summary>
    /// <param name="append">Callback that performs the append.</param>
    /// <param name="reactors">The reactors to resolve the event sequences reactor from.</param>
    /// <param name="operation">Description of the operation, used in failure messages.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="InvalidOperationException">The append was rejected, or the reactor reports an unresolved Log partition failure.</exception>
    /// <exception cref="TimeoutException">The reactor did not acknowledge the appended position within the budget.</exception>
    internal static async Task AppendAndWait(Func<Task<KernelAppendResult>> append, IReactors reactors, string operation)
    {
        const string reactorId = "$system.Cratis.Chronicle.EventSequences.EventSequencesReactor";
        var appended = await append();
        if (!appended.IsSuccess || !appended.SequenceNumber.IsActualValue)
        {
            var category = appended.IsSuccess ? "missing System position" : "append rejected";
            throw new InvalidOperationException($"Could not submit {operation}: {category}.");
        }

        var position = new EventSequenceNumber(appended.SequenceNumber.Value);
        var timeout = TimeSpan.FromSeconds(30);
        var start = Stopwatch.GetTimestamp();
        using var budget = new CancellationTokenSource(timeout);
        try
        {
            var reactor = await reactors.WaitForHandlerById(reactorId, timeout).WaitAsync(budget.Token);
            var remaining = timeout - Stopwatch.GetElapsedTime(start);
            await reactor.WaitTillReachesEventSequenceNumber(position, TimeSpan.FromTicks(Math.Max(0, remaining.Ticks))).WaitAsync(budget.Token);
            var failures = await reactor.GetFailedPartitions().WaitAsync(budget.Token);
            if (failures.Any(_ => !_.IsResolved && _.Partition.Value == KernelEventSequenceId.Log.Value))
            {
                throw new InvalidOperationException($"Could not confirm {operation}: {reactorId} has an unresolved Log partition failure at the System/{position.Value} checkpoint.");
            }
        }
        catch (OperationCanceledException error)
        {
            throw new TimeoutException($"Could not confirm {operation}: {reactorId} did not acknowledge System/{position.Value} within the checkpoint budget.", error);
        }
    }

    static Task AppendAndWait(IChronicleSetupFixture fixture, object request, string target) =>
        AppendAndWait(
            () => fixture.GetEventSequenceGrain(KernelEventSequenceId.System).Append(
                new KernelEventSourceId(KernelEventSequenceId.Log.Value),
                request,
                KernelCorrelationId.New(),
                [],
                KernelIdentity.System),
            fixture.EventStore.Reactors,
            $"{request.GetType().Name} for {target} in {fixture.EventStore.Name}/{fixture.EventStore.Namespace}");
}
