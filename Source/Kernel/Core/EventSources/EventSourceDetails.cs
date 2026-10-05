// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents the read model for a registered event source definition.
/// </summary>
/// <param name="Id">The identity of the registration, which is the name of the event source.</param>
/// <param name="Name">The name of the event source, which is the event source type written on events.</param>
/// <param name="Description">The description of the event source.</param>
/// <param name="Owner">Who owns the definition.</param>
/// <param name="Concurrency">The default concurrency dimensions of the event source.</param>
/// <param name="Streams">The event streams of the event source.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSources)]
public record EventSourceDetails(
    string Id,
    string Name,
    string Description,
    Contracts.EventSources.EventSourceOwner Owner,
    Contracts.EventSources.ConcurrencyDimensions Concurrency,
    IEnumerable<Contracts.EventSources.EventStreamDefinition> Streams)
{
    /// <summary>
    /// Gets every event source definition registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to get event sources for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event sources.</param>
    /// <returns>A collection of event source definitions.</returns>
    internal static async Task<IEnumerable<EventSourceDetails>> AllEventSources(EventStoreName eventStore, IStorage storage)
    {
        var definitions = await storage.GetEventStore(eventStore).EventSources.GetAll();
        return definitions.ToReadModel();
    }

    /// <summary>
    /// Observes every event source definition registered with an event store.
    /// </summary>
    /// <param name="eventStore">The event store to observe event sources for.</param>
    /// <param name="storage">The <see cref="IStorage"/> holding the event sources.</param>
    /// <returns>An observable subject emitting collections of event source definitions.</returns>
    internal static ISubject<IEnumerable<EventSourceDetails>> ObserveEventSources(EventStoreName eventStore, IStorage storage) =>
        storage.GetEventStore(eventStore).EventSources.ObserveAll().TransformSubject(_ => _.ToReadModel());
}
