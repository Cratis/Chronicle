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
/// <param name="connection">The Chronicle connection.</param>
/// <param name="eventTypes">The event types.</param>
/// <param name="eventSerializer">The event serializer.</param>
/// <param name="clientArtifactsProvider">The client artifacts provider.</param>
/// <param name="serviceProvider">The service provider.</param>
public class EventSeeding(
    EventStoreName eventStoreName,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    IClientArtifactsProvider clientArtifactsProvider,
    IServiceProvider serviceProvider) : IEventSeeding
{
    readonly EventStoreName _eventStoreName = eventStoreName;
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
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags, true, EventStoreNamespaceName.NotSet));
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
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags, true, EventStoreNamespaceName.NotSet));
        }
        return this;
    }

    /// <inheritdoc/>
    public IEventSeedingScopeBuilder ForNamespace(EventStoreNamespaceName @namespace)
    {
        return new EventSeedingScopeBuilder(this, false, @namespace);
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

        // Organize entries into global and namespaced groups
        var globalEntries = _entries.Where(e => e.IsGlobal).ToList();
        var namespacedEntries = _entries.Where(e => !e.IsGlobal).GroupBy(e => e.TargetNamespace);

        // Build the seed request
        var request = new Contracts.Seeding.SeedRequest
        {
            EventStore = _eventStoreName
        };

        // Process global entries
        if (globalEntries.Count > 0)
        {
            var globalByEventType = new Dictionary<EventTypeId, List<Contracts.Seeding.SeedingEntry>>();
            var globalByEventSource = new Dictionary<EventSourceId, List<Contracts.Seeding.SeedingEntry>>();

            foreach (var entry in globalEntries)
            {
                var content = await _eventSerializer.Serialize(entry.Event);
                var tags = entry.Tags?.Select(t => t.Value).ToList() ?? [];
                var contractEntry = new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = entry.EventSourceId.Value,
                    EventTypeId = entry.EventTypeId.Value,
                    Content = JsonSerializer.Serialize(content),
                    Tags = tags
                };

                if (!globalByEventType.TryGetValue(entry.EventTypeId, out var eventTypeList))
                {
                    eventTypeList = [];
                    globalByEventType[entry.EventTypeId] = eventTypeList;
                }
                eventTypeList.Add(contractEntry);

                if (!globalByEventSource.TryGetValue(entry.EventSourceId, out var eventSourceList))
                {
                    eventSourceList = [];
                    globalByEventSource[entry.EventSourceId] = eventSourceList;
                }
                eventSourceList.Add(contractEntry);
            }

            request.GlobalByEventType = globalByEventType.Select(kvp => new Contracts.Seeding.EventTypeSeedEntries
            {
                EventTypeId = kvp.Key.Value,
                Entries = kvp.Value
            }).ToList();

            request.GlobalByEventSource = globalByEventSource.Select(kvp => new Contracts.Seeding.EventSourceSeedEntries
            {
                EventSourceId = kvp.Key.Value,
                Entries = kvp.Value
            }).ToList();
        }

        // Process namespaced entries
        foreach (var namespaceGroup in namespacedEntries)
        {
            var namespacedByEventType = new Dictionary<EventTypeId, List<Contracts.Seeding.SeedingEntry>>();
            var namespacedByEventSource = new Dictionary<EventSourceId, List<Contracts.Seeding.SeedingEntry>>();

            foreach (var entry in namespaceGroup)
            {
                var content = await _eventSerializer.Serialize(entry.Event);
                var tags = entry.Tags?.Select(t => t.Value).ToList() ?? [];
                var contractEntry = new Contracts.Seeding.SeedingEntry
                {
                    EventSourceId = entry.EventSourceId.Value,
                    EventTypeId = entry.EventTypeId.Value,
                    Content = JsonSerializer.Serialize(content),
                    Tags = tags
                };

                if (!namespacedByEventType.TryGetValue(entry.EventTypeId, out var eventTypeList))
                {
                    eventTypeList = [];
                    namespacedByEventType[entry.EventTypeId] = eventTypeList;
                }
                eventTypeList.Add(contractEntry);

                if (!namespacedByEventSource.TryGetValue(entry.EventSourceId, out var eventSourceList))
                {
                    eventSourceList = [];
                    namespacedByEventSource[entry.EventSourceId] = eventSourceList;
                }
                eventSourceList.Add(contractEntry);
            }

            request.NamespacedEntries.Add(new Contracts.Seeding.NamespacedSeedEntries
            {
                Namespace = namespaceGroup.Key.Value,
                ByEventType = namespacedByEventType.Select(kvp => new Contracts.Seeding.EventTypeSeedEntries
                {
                    EventTypeId = kvp.Key.Value,
                    Entries = kvp.Value
                }).ToList(),
                ByEventSource = namespacedByEventSource.Select(kvp => new Contracts.Seeding.EventSourceSeedEntries
                {
                    EventSourceId = kvp.Key.Value,
                    Entries = kvp.Value
                }).ToList()
            });
        }

        await servicesAccessor.Services.Seeding.Seed(request);

        _entries.Clear();
    }

    void AddScopedEntry(EventSourceId eventSourceId, EventTypeId eventTypeId, object @event, IEnumerable<Tag> tags, bool isGlobal, EventStoreNamespaceName targetNamespace)
    {
        _entries.Add(new SeedingEntry(eventSourceId, eventTypeId, @event, tags, isGlobal, targetNamespace));
    }

    record SeedingEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, object Event, IEnumerable<Tag> Tags, bool IsGlobal, EventStoreNamespaceName TargetNamespace);

    class EventSeedingScopeBuilder(EventSeeding parent, bool isGlobal, EventStoreNamespaceName targetNamespace) : IEventSeedingScopeBuilder
    {
        public IEventSeedingScopeBuilder For<TEvent>(EventSourceId eventSourceId, IEnumerable<TEvent> events)
            where TEvent : class
        {
            var eventType = parent._eventTypes.GetEventTypeFor(typeof(TEvent));
            foreach (var @event in events)
            {
                var staticTags = @event.GetType().GetTags().Select(t => (Tag)t);
                parent.AddScopedEntry(eventSourceId, eventType.Id, @event, staticTags, isGlobal, targetNamespace);
            }
            return this;
        }

        public IEventSeedingScopeBuilder ForEventSource(EventSourceId eventSourceId, IEnumerable<object> events)
        {
            foreach (var @event in events)
            {
                var eventType = parent._eventTypes.GetEventTypeFor(@event.GetType());
                var staticTags = @event.GetType().GetTags().Select(t => (Tag)t);
                parent.AddScopedEntry(eventSourceId, eventType.Id, @event, staticTags, isGlobal, targetNamespace);
            }
            return this;
        }
    }
}
