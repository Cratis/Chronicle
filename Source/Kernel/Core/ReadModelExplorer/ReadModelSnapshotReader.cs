// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Sequences;
using Cratis.Chronicle.Storage;
using AppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;

namespace Cratis.Chronicle.ReadModelExplorer;

/// <summary>
/// Reads the snapshots a read model instance passed through, by replaying the events behind it.
/// </summary>
internal static class ReadModelSnapshotReader
{
    /// <summary>
    /// Folds an instance's events into snapshots, grouped the way the caller asked for.
    /// </summary>
    /// <param name="readModelIdentifier">The read model to read snapshots of.</param>
    /// <param name="eventStore">The event store the read model lives in.</param>
    /// <param name="namespace">The namespace the read model lives in.</param>
    /// <param name="eventSequenceId">The event sequence to read from.</param>
    /// <param name="readModelKey">The key of the instance to fold.</param>
    /// <param name="grouping">How to group the events into snapshots.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to resolve the read model and its projection with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read events and definitions from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> to render the state with.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> content is serialized with.</param>
    /// <returns>The snapshots, oldest first.</returns>
    /// <remarks>
    /// Only a projection-backed read model has snapshots the server can produce - a reducer runs in the client
    /// that declared it, so the server has nothing to fold and answers with none.
    /// <para>
    /// Both groupings are one pass over the same events, carrying the state forward - they differ only in how
    /// often a snapshot is taken from it.
    /// </para>
    /// </remarks>
    internal static async Task<IEnumerable<ReadModelSnapshot>> Read(
        Concepts.ReadModels.ReadModelIdentifier readModelIdentifier,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string readModelKey,
        ReadModelSnapshotGrouping grouping,
        IGrainFactory grainFactory,
        IStorage storage,
        IEventCompliance eventCompliance,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions)
    {
        var readModel = grainFactory.GetReadModel(readModelIdentifier, eventStore);
        var definition = await readModel.GetDefinition();

        if (definition.ObserverType != Concepts.ReadModels.ReadModelObserverType.Projection)
        {
            return [];
        }

        var history = await ReadHistory(
            definition.ObserverIdentifier,
            eventStore,
            @namespace,
            eventSequenceId,
            readModelKey,
            grainFactory,
            storage,
            eventCompliance);

        IEnumerable<(CorrelationId CorrelationId, List<AppendedEvent> Events)> groups = grouping == ReadModelSnapshotGrouping.Event
            ? history.Events.Select(appendedEvent => (appendedEvent.Context.CorrelationId, new List<AppendedEvent> { appendedEvent }))
            : GroupByCorrelation(history.Events);

        var snapshots = new List<ReadModelSnapshot>();
        var state = new ExpandoObject();

        foreach (var (correlationId, events) in groups)
        {
            state = await history.Projection.ProcessForSingleReadModel(@namespace, state, events);

            snapshots.Add(new ReadModelSnapshot(
                events[0].Context.Occurred,
                correlationId,
                Serialize(state, history.ReadModelDefinition, expandoObjectConverter, jsonSerializerOptions),
                events.Select(appendedEvent => new Event(
                    appendedEvent.Context.ToApi(),
                    JsonSerializer.Serialize(appendedEvent.Content, jsonSerializerOptions)))));
        }

        return snapshots;
    }

    /// <summary>
    /// Reads the events that shaped one read model instance, decrypted and in order.
    /// </summary>
    /// <param name="projectionId">The projection that produces the read model.</param>
    /// <param name="eventStore">The event store the read model lives in.</param>
    /// <param name="namespace">The namespace the read model lives in.</param>
    /// <param name="eventSequenceId">The event sequence to read from.</param>
    /// <param name="readModelKey">The key of the instance to read the history of.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to resolve the projection with.</param>
    /// <param name="storage">The <see cref="IStorage"/> to read events and definitions from.</param>
    /// <param name="eventCompliance">The <see cref="IEventCompliance"/> to release PII content with.</param>
    /// <returns>The events behind the instance, with what is needed to project them.</returns>
    /// <remarks>
    /// A read model's key is only the event source id when the projection does not say otherwise. Where it does -
    /// a projection keyed on an event property, say - narrowing the sequence by event source finds nothing, and
    /// the instance looks like it has no history at all. Narrowing stays the fast path for the projections it is
    /// actually right for; the rest read the sequence and let the projection say which events resolved to this key.
    /// </remarks>
    static async Task<ProjectedReadModelHistory> ReadHistory(
        string projectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string readModelKey,
        IGrainFactory grainFactory,
        IStorage storage,
        IEventCompliance eventCompliance)
    {
        var eventSequenceStorage = storage
            .GetEventStore(eventStore)
            .GetNamespace(@namespace)
            .GetEventSequence(eventSequenceId);

        var projection = grainFactory.GetGrain<IProjection>(new ProjectionKey(projectionId, eventStore));
        var definition = await projection.GetDefinition();
        var readModelDefinition = await storage.GetEventStore(eventStore).ReadModels.Get(definition.ReadModel);
        var eventTypes = await projection.GetEventTypes();

        var keyIsEventSourceId = definition.From.Values.All(from => from.Key is null || string.IsNullOrEmpty(from.Key.Value));

        var cursor = keyIsEventSourceId
            ? await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, readModelKey, eventTypes: eventTypes)
            : await eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: eventTypes);

        var allEvents = new List<AppendedEvent>();
        while (await cursor.MoveNext())
        {
            allEvents.AddRange(cursor.Current);
        }
        cursor.Dispose();

        if (!keyIsEventSourceId)
        {
            allEvents = [.. await projection.GetEventsForKey(@namespace, readModelKey, allEvents)];
        }

        // Decrypt the stored events before projecting and returning them - both the projected read
        // model and the events it carries must be released so no PII leaves encrypted.
        var eventTypeSchemas = await storage.GetEventStore(eventStore).EventTypes.GetFor(allEvents.Select(_ => _.Context.EventType).Distinct());
        var releasedEvents = await eventCompliance.Release(allEvents, eventTypeSchemas.ToDictionary(_ => _.Type));

        return new(projection, readModelDefinition, [.. releasedEvents.OrderBy(_ => _.Context.SequenceNumber)]);
    }

    /// <summary>
    /// Groups events by the correlation they were appended under, keeping the order they arrived in.
    /// </summary>
    /// <param name="events">The events to group.</param>
    /// <returns>Each correlation with its events, ordered.</returns>
    static IEnumerable<(CorrelationId CorrelationId, List<AppendedEvent> Events)> GroupByCorrelation(IEnumerable<AppendedEvent> events) =>
        events
            .GroupBy(appendedEvent => appendedEvent.Context.CorrelationId)
            .Select(group => (group.Key, group.OrderBy(appendedEvent => appendedEvent.Context.SequenceNumber).ToList()));

    static string Serialize(
        ExpandoObject state,
        Concepts.ReadModels.ReadModelDefinition readModelDefinition,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions) =>
        JsonSerializer.Serialize(
            expandoObjectConverter.ToJsonObject(state, readModelDefinition.GetSchemaForLatestGeneration()),
            jsonSerializerOptions);

    /// <summary>
    /// The events behind one read model instance, with what is needed to project them.
    /// </summary>
    /// <param name="Projection">The projection that produces the read model.</param>
    /// <param name="ReadModelDefinition">The read model's definition, for serializing against its schema.</param>
    /// <param name="Events">The events that shaped the instance, oldest first and decrypted.</param>
    record ProjectedReadModelHistory(
        IProjection Projection,
        Concepts.ReadModels.ReadModelDefinition ReadModelDefinition,
        IReadOnlyList<AppendedEvent> Events);
}
