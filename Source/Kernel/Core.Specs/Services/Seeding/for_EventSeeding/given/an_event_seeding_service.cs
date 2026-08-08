// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Contracts.Seeding;

using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;
using SeedingGrain = Cratis.Chronicle.Seeding.IEventSeeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.given;

public class an_event_seeding_service : Specification
{
    protected const string TheEventStore = "TestEventStore";
    protected const string TheNamespace = "TestNamespace";

    protected IGrainFactory _grainFactory;
    protected SeedingGrain _globalGrain;
    protected SeedingGrain _namespaceGrain;
    protected Contracts.Seeding.IEventSeeding _service;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _globalGrain = Substitute.For<SeedingGrain>();
        _namespaceGrain = Substitute.For<SeedingGrain>();

        _globalGrain.Seed(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(Chronicle.Seeding.SeedingResult.Complete));
        _namespaceGrain.Seed(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(Chronicle.Seeding.SeedingResult.Complete));

        _grainFactory.GetGrain<SeedingGrain>(EventSeedingKey.ForGlobal(TheEventStore).ToString()).Returns(_globalGrain);
        _grainFactory.GetGrain<SeedingGrain>(EventSeedingKey.ForNamespace(TheEventStore, TheNamespace).ToString()).Returns(_namespaceGrain);

        _service = new EventSeeding(_grainFactory);
    }

    /// <summary>
    /// Gets the entries that reached the global grain, in the order they were handed over.
    /// </summary>
    protected IEnumerable<SeededEntry> EntriesSeededGlobally => EntriesHandedTo(_globalGrain);

    /// <summary>
    /// Gets the entries that reached the namespace grain, in the order they were handed over.
    /// </summary>
    protected IEnumerable<SeededEntry> EntriesSeededForTheNamespace => EntriesHandedTo(_namespaceGrain);

    /// <summary>
    /// Builds a contract entry, the shape the client puts on the wire.
    /// </summary>
    /// <param name="eventSourceId">The event source to build for.</param>
    /// <param name="eventTypeId">The event type to build for.</param>
    /// <param name="content">The JSON content to build for.</param>
    /// <param name="tags">Optional tags to build for.</param>
    /// <returns>A <see cref="SeedingEntry"/>.</returns>
    protected static SeedingEntry AnEntry(string eventSourceId, string eventTypeId, string content, params string[] tags) =>
        new()
        {
            EventSourceId = eventSourceId,
            EventTypeId = eventTypeId,
            Content = content,
            Tags = [.. tags]
        };

    /// <summary>
    /// Builds the request the client builds: every entry is bucketed by its event type AND by its event
    /// source, so the very same entry travels twice - once in each grouping.
    /// </summary>
    /// <param name="entries">The entries the seeders yielded, in order.</param>
    /// <returns>A <see cref="SeedRequest"/> for the global scope.</returns>
    protected static SeedRequest AGlobalRequestFor(params SeedingEntry[] entries) =>
        new()
        {
            EventStore = TheEventStore,
            GlobalByEventType = ByEventType(entries),
            GlobalByEventSource = ByEventSource(entries)
        };

    /// <summary>
    /// The namespaced half of the same shape.
    /// </summary>
    /// <param name="entries">The entries the seeders yielded, in order.</param>
    /// <returns>A <see cref="SeedRequest"/> for a single namespace.</returns>
    protected static SeedRequest ANamespacedRequestFor(params SeedingEntry[] entries) =>
        new()
        {
            EventStore = TheEventStore,
            NamespacedEntries =
            [
                new NamespacedSeedEntries
                {
                    Namespace = TheNamespace,
                    ByEventType = ByEventType(entries),
                    ByEventSource = ByEventSource(entries)
                }
            ]
        };

    static List<EventTypeSeedEntries> ByEventType(IEnumerable<SeedingEntry> entries) =>
        [.. entries.GroupBy(_ => _.EventTypeId).Select(group => new EventTypeSeedEntries { EventTypeId = group.Key, Entries = [.. group] })];

    static List<EventSourceSeedEntries> ByEventSource(IEnumerable<SeedingEntry> entries) =>
        [.. entries.GroupBy(_ => _.EventSourceId).Select(group => new EventSourceSeedEntries { EventSourceId = group.Key, Entries = [.. group] })];

    static IEnumerable<SeededEntry> EntriesHandedTo(SeedingGrain grain) =>
        grain.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(SeedingGrain.Seed))
            .SelectMany(call => (IEnumerable<SeededEntry>)call.GetArguments()[0]!);
}
