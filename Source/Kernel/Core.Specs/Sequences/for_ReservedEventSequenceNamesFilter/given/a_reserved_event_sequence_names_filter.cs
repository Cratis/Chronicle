// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceNamesFilter.given;

public class a_reserved_event_sequence_names_filter : Specification, IDisposable
{
    protected const string EventStore = "some-event-store";
    protected const string Namespace = "some-namespace";

    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IReservedEventSequenceIds _reservedEventSequenceIds;
    internal ReservedEventSequenceNamesFilter _filter;
    protected ServiceProvider _services;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _reservedEventSequenceIds = Substitute.For<IReservedEventSequenceIds>();

        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([(EventStoreName)EventStore]));
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([]));

        _services = new ServiceCollection()
            .AddSingleton(_storage)
            .AddSingleton(_reservedEventSequenceIds)
            .BuildServiceProvider();
        _filter = new ReservedEventSequenceNamesFilter(_services);
    }

    public void Dispose() => _services.Dispose();

    protected void Reserve(string eventSequenceId) =>
        _reservedEventSequenceIds.IsReserved(new EventSequenceId(eventSequenceId)).Returns(true);

    protected static CommandContext ContextFor(object command) =>
        new(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues());

    protected static Append AppendTo(string eventSequenceId) => new(
        EventStore,
        Namespace,
        eventSequenceId,
        "some-event-source",
        "Default",
        "All",
        "Default",
        new EventType("SomeEvent", 1, false),
        "{}");
}
