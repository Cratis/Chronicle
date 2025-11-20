// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
public class EventSeeding : IEventSeeding
{
    readonly EventStoreName _eventStoreName;
    readonly EventStoreNamespaceName _namespace;
    readonly IChronicleConnection _connection;
    readonly IEventTypes _eventTypes;
    readonly IEventSerializer _eventSerializer;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider;
    readonly List<SeedingEntry> _entries = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSeeding"/> class.
    /// </summary>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="connection">The Chronicle connection.</param>
    /// <param name="eventTypes">The event types.</param>
    /// <param name="eventSerializer">The event serializer.</param>
    /// <param name="clientArtifactsProvider">The client artifacts provider.</param>
    /// <param name="serviceProvider">The service provider.</param>
    public EventSeeding(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        IChronicleConnection connection,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider)
    {
        _eventStoreName = eventStoreName;
        _namespace = @namespace;
        _connection = connection;
        _eventTypes = eventTypes;
        _eventSerializer = eventSerializer;
        _clientArtifactsProvider = clientArtifactsProvider;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
        where TEvent : class
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        foreach (var @event in events)
        {
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event));
        }
        return this;
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event));
        }
        return this;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        // Discovery happens through IClientArtifactsProvider, no additional work needed here
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        // Invoke all discovered seeders
        foreach (var seederType in _clientArtifactsProvider.EventSeeders)
        {
            var seeder = _serviceProvider.GetService(seederType) as ICanSeedEvents;
            seeder?.Seed(this);
        }

        // Send all collected entries to the server
        if (_entries.Count == 0)
        {
            return;
        }

        var servicesAccessor = (IChronicleServicesAccessor)_connection;
        var serializedEntries = new List<SerializedSeedingEntry>();

        foreach (var entry in _entries)
        {
            var content = await _eventSerializer.Serialize(entry.Event);
            serializedEntries.Add(new SerializedSeedingEntry(
                entry.EventSourceId.Value,
                entry.EventTypeId.Value,
                JsonSerializer.Serialize(content)));
        }

        await servicesAccessor.Services.Seeding.Seed(
            new Contracts.Seeding.SeedRequest
            {
                EventStore = _eventStoreName,
                Namespace = _namespace,
                Entries = serializedEntries.Select(e => new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = e.EventSourceId,
                    EventTypeId = e.EventTypeId,
                    Content = e.Content
                }).ToList()
            });

        _entries.Clear();
    }

    record SeedingEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, object Event);
    record SerializedSeedingEntry(string EventSourceId, string EventTypeId, string Content);
}
