// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;

using ContractSeedingEntry = Cratis.Chronicle.Contracts.Seeding.SeedingEntry;
using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;
using SeedingGrain = Cratis.Chronicle.Seeding.IResultAwareEventSeeding;

namespace Cratis.Chronicle.Seeding.for_SeedEvents.given;

public class a_seeding_grain : Specification
{
    protected const string TheEventStore = "TestEventStore";
    protected const string TheNamespace = "TestNamespace";

    protected IGrainFactory _grainFactory;
    protected SeedingGrain _globalGrain;
    protected SeedingGrain _namespaceGrain;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _globalGrain = Substitute.For<SeedingGrain>();
        _namespaceGrain = Substitute.For<SeedingGrain>();

        _globalGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));

        _grainFactory.GetGrain<SeedingGrain>(EventSeedingKey.ForGlobal(TheEventStore).ToString()).Returns(_globalGrain);
        _grainFactory.GetGrain<SeedingGrain>(EventSeedingKey.ForNamespace(TheEventStore, TheNamespace).ToString()).Returns(_namespaceGrain);
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
    /// <returns>A <see cref="ContractSeedingEntry"/>.</returns>
    protected static ContractSeedingEntry AnEntry(string eventSourceId, string eventTypeId, string content, params string[] tags) =>
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
    /// <returns>A <see cref="SeedEvents"/> for the global scope.</returns>
    protected static SeedEvents AGlobalRequestFor(params ContractSeedingEntry[] entries) =>
        new(TheEventStore, ByEventType(entries), ByEventSource(entries), []);

    /// <summary>
    /// The namespaced half of the same shape.
    /// </summary>
    /// <param name="entries">The entries the seeders yielded, in order.</param>
    /// <returns>A <see cref="SeedEvents"/> for a single namespace.</returns>
    protected static SeedEvents ANamespacedRequestFor(params ContractSeedingEntry[] entries) =>
        new(
            TheEventStore,
            [],
            [],
            [
                new Contracts.Seeding.NamespacedSeedEntries
                {
                    Namespace = TheNamespace,
                    ByEventType = ByEventType(entries),
                    ByEventSource = ByEventSource(entries)
                }
            ]);

    static List<Contracts.Seeding.EventTypeSeedEntries> ByEventType(IEnumerable<ContractSeedingEntry> entries) =>
        [.. entries.GroupBy(_ => _.EventTypeId).Select(group => new Contracts.Seeding.EventTypeSeedEntries { EventTypeId = group.Key, Entries = [.. group] })];

    static List<Contracts.Seeding.EventSourceSeedEntries> ByEventSource(IEnumerable<ContractSeedingEntry> entries) =>
        [.. entries.GroupBy(_ => _.EventSourceId).Select(group => new Contracts.Seeding.EventSourceSeedEntries { EventSourceId = group.Key, Entries = [.. group] })];

    static IEnumerable<SeededEntry> EntriesHandedTo(SeedingGrain grain) =>
        grain.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(SeedingGrain.SeedWithResult))
            .SelectMany(call => (IEnumerable<SeededEntry>)call.GetArguments()[0]!);
}
