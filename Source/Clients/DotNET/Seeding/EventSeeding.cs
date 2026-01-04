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
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags, false, _namespace));
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
            _entries.Add(new SeedingEntry(eventSourceId, eventType.Id, @event, staticTags, false, _namespace));
        }
        return this;
    }

    /// <inheritdoc/>
    public IEventSeedingScopeBuilder ForAllNamespaces()
    {
        return new EventSeedingScopeBuilder(this, true, EventStoreNamespaceName.NotSet);
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
        var serializedEntries = new List<SerializedSeedingEntry>();

        foreach (var entry in _entries)
        {
            var content = await _eventSerializer.Serialize(entry.Event);
            var tags = entry.Tags?.Select(t => t.Value).ToList() ?? [];
            serializedEntries.Add(new SerializedSeedingEntry(
                entry.EventSourceId.Value,
                entry.EventTypeId.Value,
                JsonSerializer.Serialize(content),
                tags,
                entry.IsGlobal,
                entry.TargetNamespace.Value));
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
                    Tags = e.Tags,
                    IsGlobal = e.IsGlobal,
                    TargetNamespace = e.TargetNamespace
                })
            });

        _entries.Clear();
    }

    void AddScopedEntry(EventSourceId eventSourceId, EventTypeId eventTypeId, object @event, IEnumerable<Tag> tags, bool isGlobal, EventStoreNamespaceName targetNamespace)
    {
        _entries.Add(new SeedingEntry(eventSourceId, eventTypeId, @event, tags, isGlobal, targetNamespace));
    }

    record SeedingEntry(EventSourceId EventSourceId, EventTypeId EventTypeId, object Event, IEnumerable<Tag> Tags, bool IsGlobal, EventStoreNamespaceName TargetNamespace);
    record SerializedSeedingEntry(string EventSourceId, string EventTypeId, string Content, IList<string> Tags, bool IsGlobal, string TargetNamespace);

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
