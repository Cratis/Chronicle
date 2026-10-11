// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Testing.Events;
using KernelEvents = KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Preserves synthetic scenario contexts while preparing generation-aware kernel delivery.
/// </summary>
internal static class ReadModelEvents
{
    /// <summary>
    /// Gets the sequence used for synthetic input, separate from events appended by application code.
    /// </summary>
    internal static readonly KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId SequenceId = new("testing-read-model-input");

    /// <summary>
    /// Migrates seeded input and stores each sequence slot only once across repeated folds.
    /// </summary>
    /// <param name="store">The shared test store.</param>
    /// <param name="events">Seeded input.</param>
    /// <param name="contexts">Optional original client contexts.</param>
    /// <param name="persist">Whether to persist synthetic input; false uses the existing event log.</param>
    /// <returns>The delivered kernel events and their shared storage.</returns>
    /// <exception cref="SyntheticEventAppendFailed">A synthetic position was already occupied.</exception>
    internal static async Task<(KernelEvents::AppendedEvent[] Events, IEventSequenceStorage Storage)> Prepare(
        EventStoreForTesting store,
        IReadOnlyList<(EventSourceId EventSourceId, object Event)> events,
        IReadOnlyList<EventContext>? contexts = null,
        bool persist = true)
    {
        var root = store.TestingStore;
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var migrations = new KernelSequences::Migrations.EventTypeMigrations(root.Storage, converter);
        var calculator = new KernelSequences::EventHashCalculator();
        var storage = root.Store.GetNamespace(new(root.Namespace.Value)).GetEventSequence(persist ? SequenceId : KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceId.Log);
        var storedCount = (await storage.GetCount()).Value;
        var result = new KernelEvents::AppendedEvent[events.Count];
        for (var index = 0; index < events.Count; index++)
        {
            var input = events[index];
            var type = store.EventTypes.GetEventTypeFor(input.Event.GetType());
            var kernelType = new KernelEvents::EventType(new(type.Id.Value), new(type.Generation.Value));
            var schema = await root.Store.EventTypes.GetFor(kernelType.Id, kernelType.Generation);
            var serialized = await store.EventSerializer.Serialize(input.Event);

            // Projection keys are case-sensitive even if the client serializer isn't.
            var json = JsonNode.Parse(serialized.ToJsonString(), new JsonNodeOptions { PropertyNameCaseInsensitive = false })!.AsObject();
            var content = converter.ToExpandoObject(json, schema.Schema);
            var generations = await migrations.MigrateToAllGenerations(new KernelConcepts::Cratis.Chronicle.Concepts.EventStoreName(root.Name.Value), kernelType, json, content);
            var context = KernelEvents::EventContext.Empty with
            {
                EventType = kernelType,
                EventSourceId = new(input.EventSourceId.Value),
                EventSourceType = contexts is null ? KernelEvents::EventSourceType.Default : new(contexts[index].EventSourceType.Value),
                EventStreamType = contexts is null ? KernelEvents::EventStreamType.All : new(contexts[index].EventStreamType.Value),
                EventStreamId = contexts is null ? KernelEvents::EventStreamId.Default : new(contexts[index].EventStreamId.Value),
                SequenceNumber = contexts is null ? new((ulong)index) : new(contexts[index].SequenceNumber.Value),
                Occurred = contexts is null ? KernelEvents::EventContext.Empty.Occurred.AddTicks(index) : contexts[index].Occurred
            };
            var hashes = generations.ToDictionary(pair => pair.Key, pair => calculator.Calculate(kernelType.Id, context.EventSourceId, pair.Value));
            if (persist && (ulong)index >= storedCount)
            {
                var appended = await storage.Append(context.SequenceNumber, context.EventSourceType, context.EventSourceId, context.EventStreamType, context.EventStreamId, kernelType, context.CorrelationId, context.Causation, [], context.Tags, context.Occurred, generations, hashes);
                if (appended.TryGetError(out var error)) throw new SyntheticEventAppendFailed(error.ToString()!);
            }
            var latest = store.EventTypes.All.Where(candidate => candidate.Id == type.Id).MaxBy(candidate => candidate.Generation.Value)!;
            var generation = new KernelEvents::EventTypeGeneration(latest.Generation.Value);
            result[index] = new(context with { EventType = new(kernelType.Id, generation) }, generations[generation]);
        }

        return (result, storage);
    }
}
