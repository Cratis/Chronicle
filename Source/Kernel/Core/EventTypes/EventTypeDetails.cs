// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the read model for a registered event type and the schema of its latest generation.
/// </summary>
/// <param name="Type">The event type and the generation this registration is for.</param>
/// <param name="Owner">Who owns the event type.</param>
/// <param name="Source">Where the event type came from.</param>
/// <param name="Schema">The JSON schema of the event type at this generation.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventTypes)]
public record EventTypeDetails(
    Contracts.Events.EventType Type,
    Contracts.Events.EventTypeOwner Owner,
    Contracts.Events.EventTypeSource Source,
    string Schema)
{
    /// <summary>
    /// Gets the latest generation of every event type registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get event types for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <returns>A collection of event type registrations.</returns>
    internal static async Task<IEnumerable<EventTypeDetails>> AllEventTypes(string eventStore, IStorage storage)
    {
        var eventTypes = await storage.GetEventStore(eventStore).EventTypes.GetLatestForAllEventTypes();
        return eventTypes.ToReadModel();
    }

    /// <summary>
    /// Observes the latest generation of every event type registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to observe event types for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <returns>An observable subject emitting collections of event type registrations.</returns>
    internal static ISubject<IEnumerable<EventTypeDetails>> ObserveEventTypes(string eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).EventTypes.ObserveLatestForAllEventTypes().TransformSubject(_ => _.ToReadModel());

    /// <summary>
    /// Gets every generation registered for one event type.
    /// </summary>
    /// <param name="eventStore">The event store the event type belongs to.</param>
    /// <param name="eventTypeId">The event type to get generations for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <returns>A collection of registrations, one per generation.</returns>
    internal static async Task<IEnumerable<EventTypeDetails>> AllEventTypeGenerations(string eventStore, string eventTypeId, IStorage storage)
    {
        var eventType = new EventType(eventTypeId, EventTypeGeneration.First, false);
        var schemas = await storage.GetEventStore(eventStore).EventTypes.GetAllGenerationsForEventType(eventType);
        return schemas.ToReadModel();
    }
}
