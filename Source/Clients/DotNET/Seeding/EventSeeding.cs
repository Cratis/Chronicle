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
/// <remarks>
/// Initializes a new instance of the <see cref="EventSeeding"/> class.
/// </remarks>
/// <param name="eventStoreName">The event store name.</param>
/// <param name="namespace">The namespace.</param>
/// <param name="connection">The Chronicle connection.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="eventSerializer">The event serializer.</param>
/// <param name="clientArtifactsProvider">The client artifacts provider.</param>
/// <param name="serviceProvider">The service provider.</param>
public class EventSeeding(
    EventStoreName eventStoreName,
    EventStoreNamespaceName @namespace,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    IClientArtifactsProvider clientArtifactsProvider,
    IServiceProvider serviceProvider) : IEventSeeding
{
    readonly EventStoreName _eventStoreName = eventStoreName;
    readonly EventStoreNamespaceName _namespace = @namespace;
    readonly IChronicleConnection _connection = connection;
    readonly IEventTypes _eventTypes = eventTypes;
    readonly IEventSerializer _eventSerializer = eventSerializer;
    readonly IClientArtifactsProvider _clientArtifactsProvider = clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider = serviceProvider;
    readonly List<SeedingEntry> _entries = [];

    /// <inheritdoc/>
    public IEventSeedingBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
        where TEvent : class
    {
        var eventType = _eventTypes.GetEventTypeFor(typeof(TEvent));
        foreach (var @event in events)
        {
            var eventType_clrType = @event.GetType();
            var staticTags = eventType_clrType.GetTags().Select(t => (Tag)t);
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags));
        }
        return this;
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events)
    {
        foreach (var @event in events)
        {
            var eventType = _eventTypes.GetEventTypeFor(@event.GetType());
            var staticTags = @event.GetType().GetTags().Select(t => (Tag)t);
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags));
        }
        return this;
    }

    /// <inheritdoc/>
    public Task Discover()
    {
        foreach (var seederType in _clientArtifactsProvider.EventSeeders)
        {
            var seeder = _serviceProvider.GetService(seederType) as ICanSeedEvents;
            seeder?.Seed(this);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Register()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        var servicesAccessor = (IChronicleServicesAccessor)_connection;
        var serializedEntries = new List<SerializedSeedingEntry>();

        foreach (var entry in _entries)
        {
            var content = await _eventSerializer.Serialize(entry.Event);
            var tags = entry.Tags?.Select(t => t.Value).ToList() ?? [];
            serializedEntries.Add(new SerializedSeedingEntry(
                entry.EventSourceId.Value,
                entry.EventTypeId.Value,
                JsonSerializer.Serialize(content),
                tags));
        }

        await servicesAccessor.Services.Seeding.Seed(
            new Contracts.Seeding.SeedRequest
            {
                EventStore = _eventStoreName,
                Namespace = _namespace,
                Entries = serializedEntries.ConvertAll(e => new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = e.EventSourceId,
                    EventTypeId = e.EventTypeId,
                    Content = e.Content,
                    Tags = e.Tags
                })
            });

        _entries.Clear();
    }

    record SeedingEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, object Event, IEnumerable<Tag> Tags);
    record SerializedSeedingEntry(string EventSourceId, string EventTypeId, string Content, IList<string> Tags);
}
