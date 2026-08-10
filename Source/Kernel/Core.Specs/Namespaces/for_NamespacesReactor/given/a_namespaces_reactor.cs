// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor.given;

public class a_namespaces_reactor : Specification
{
    protected static readonly EventStoreName EventStore = "event-store";
    protected static readonly EventStoreNamespaceName Namespace = "new-namespace";

    protected IResultAwareEventSeeding _globalGrain;
    protected IResultAwareEventSeeding _namespaceGrain;
    protected NamespacesReactor _reactor;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        _globalGrain = Substitute.For<IResultAwareEventSeeding>();
        _namespaceGrain = Substitute.For<IResultAwareEventSeeding>();
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));

        grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForGlobal(EventStore).ToString()).Returns(_globalGrain);
        grainFactory.GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace(EventStore, Namespace).ToString()).Returns(_namespaceGrain);

        _reactor = new NamespacesReactor(grainFactory);
    }

    /// <summary>
    /// Sets the global entries the new namespace should receive, preserving their given order.
    /// </summary>
    /// <param name="entries">Entries to expose from global seeding state.</param>
    protected void GlobalSeeds(params SeededEventEntry[] entries)
    {
        var byEventSource = entries
            .GroupBy(_ => _.EventSourceId)
            .ToDictionary(_ => _.Key, _ => _.AsEnumerable());
        var byEventType = entries
            .GroupBy(_ => _.EventTypeId)
            .ToDictionary(_ => _.Key, _ => _.AsEnumerable());

        _globalGrain.GetSeededEvents().Returns(Task.FromResult(new EventSeeds(byEventType, byEventSource)));
    }

    /// <summary>
    /// Gets the entries offered to the new namespace in order.
    /// </summary>
    protected IEnumerable<SeedingEntry> EntriesOfferedToTheNamespace =>
        _namespaceGrain.ReceivedCalls()
            .Where(_ => _.GetMethodInfo().Name == nameof(IResultAwareEventSeeding.SeedWithResult))
            .SelectMany(_ => (IEnumerable<SeedingEntry>)_.GetArguments()[0]!);

    /// <summary>
    /// Creates a stored global seed entry.
    /// </summary>
    /// <param name="tags">Tags the entry carries.</param>
    /// <returns>A stored seed entry.</returns>
    protected static SeededEventEntry AnEntry(params string[] tags) =>
        new("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}", tags);

    /// <summary>
    /// Invokes the reactor for the shared new namespace.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected Task AddNamespace() => _reactor.Added(new NamespaceAdded(EventStore, Namespace), EventContext.Empty);
}
