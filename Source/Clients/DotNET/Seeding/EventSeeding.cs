// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Grpc.Core;
using Microsoft.Extensions.Logging;

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
/// <param name="artifactActivator">The artifact activator.</param>
/// <param name="logger">The logger.</param>
public class EventSeeding(
    EventStoreName eventStoreName,
    IChronicleConnection connection,
    IEventTypes eventTypes,
    IEventSerializer eventSerializer,
    IClientArtifactsProvider clientArtifactsProvider,
    IServiceProvider serviceProvider,
    IClientArtifactsActivator artifactActivator,
    ILogger<EventSeeding> logger) : IEventSeeding
{
    readonly EventStoreName _eventStoreName = eventStoreName;
    readonly IChronicleConnection _connection = connection;
    readonly IEventTypes _eventTypes = eventTypes;
    readonly IEventSerializer _eventSerializer = eventSerializer;
    readonly IClientArtifactsProvider _clientArtifactsProvider = clientArtifactsProvider;
    readonly IServiceProvider _serviceProvider = serviceProvider;
    readonly IClientArtifactsActivator _artifactActivator = artifactActivator;
    readonly ILogger<EventSeeding> _logger = logger;
    readonly List<SeedingEntry> _entries = [];
    readonly IEventSources? _eventSources;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSeeding"/> class with typed routing support.
    /// </summary>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="connection">The Chronicle connection.</param>
    /// <param name="eventTypes">The event types.</param>
    /// <param name="eventSerializer">The event serializer.</param>
    /// <param name="clientArtifactsProvider">The client artifacts provider.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="artifactActivator">The artifact activator.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="eventSources">The event source definitions.</param>
    public EventSeeding(
        EventStoreName eventStoreName,
        IChronicleConnection connection,
        IEventTypes eventTypes,
        IEventSerializer eventSerializer,
        IClientArtifactsProvider clientArtifactsProvider,
        IServiceProvider serviceProvider,
        IClientArtifactsActivator artifactActivator,
        ILogger<EventSeeding> logger,
        IEventSources? eventSources)
        : this(eventStoreName, connection, eventTypes, eventSerializer, clientArtifactsProvider, serviceProvider, artifactActivator, logger)
    {
        _eventSources = eventSources;
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder For<TEvent>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<TEvent> events, EventSourceType? eventSourceType = default)
        where TEvent : class
    {
        var routed = events.Select(@event => new EventForEventSourceId(eventSourceId, @event)
        {
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId
        });
        AddRoutedEntries(routed, true, EventStoreNamespaceName.NotSet, _eventTypes.GetEventTypeFor(typeof(TEvent)).Id);
        return this;
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder ForEventSource(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<object> events, EventSourceType? eventSourceType = default)
    {
        var routed = events.Select(@event => new EventForEventSourceId(eventSourceId, @event)
        {
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            EventStreamType = eventStreamType,
            EventStreamId = eventStreamId
        });
        return ForEvents(routed);
    }

    /// <inheritdoc/>
    public IEventSeedingBuilder ForEvents(IEnumerable<EventForEventSourceId> events)
    {
        AddRoutedEntries(events, true, EventStoreNamespaceName.NotSet);
        return this;
    }

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
    public async Task Discover()
    {
        foreach (var seederType in _clientArtifactsProvider.EventSeeders)
        {
            var activatedSeederResult = _artifactActivator.Activate<ICanSeedEvents>(_serviceProvider, seederType);
            if (activatedSeederResult.TryGetException(out var exception))
            {
                _logger.FailedToActivateSeeder(seederType, exception);
                continue;
            }

            await using var activatedSeeder = activatedSeederResult.AsT0;
            activatedSeeder.Instance.Seed(this);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="EventSeedingRoutingNotSupported">Thrown before sending entries when routing support is not confirmed by the kernel.</exception>
    public async Task Register()
    {
        if (_entries.Count == 0)
        {
            return;
        }

        var servicesAccessor = (IChronicleServicesAccessor)_connection;
        if (_entries.Exists(entry =>
            (!string.IsNullOrEmpty(entry.EventSourceType?.Value) && entry.EventSourceType != EventSourceType.Default) ||
            (!string.IsNullOrEmpty(entry.EventStreamType?.Value) && entry.EventStreamType != EventStreamType.All) ||
            (!string.IsNullOrEmpty(entry.EventStreamId?.Value) && entry.EventStreamId.Value != EventStreamId.Default)))
        {
            try
            {
                var support = await servicesAccessor.Services.Seeding.GetSeedingSupport().EnsureSuccess();
                if (!support.RoutingSupported)
                {
                    throw new EventSeedingRoutingNotSupported();
                }
            }
            catch (RpcException exception) when (exception.StatusCode == StatusCode.Unimplemented)
            {
                throw new EventSeedingRoutingNotSupported();
            }
        }

        // Organize entries into global and namespaced groups
        var globalEntries = _entries.Where(e => e.IsGlobal).ToList();
        var namespacedEntries = _entries.Where(e => !e.IsGlobal).GroupBy(e => e.TargetNamespace);

        var globalByEventTypeEntries = new List<Contracts.Seeding.EventTypeSeedEntries>();
        var globalByEventSourceEntries = new List<Contracts.Seeding.EventSourceSeedEntries>();
        var namespaced = new List<Contracts.Seeding.NamespacedSeedEntries>();

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
                    Tags = tags,
                    EventSourceType = entry.EventSourceType?.Value ?? EventSourceType.Default.Value,
                    EventStreamType = entry.EventStreamType?.Value ?? EventStreamType.All.Value,
                    EventStreamId = entry.EventStreamId?.Value ?? EventStreamId.Default
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

            globalByEventTypeEntries.AddRange(globalByEventType.Select(kvp => new Contracts.Seeding.EventTypeSeedEntries
            {
                EventTypeId = kvp.Key.Value,
                Entries = kvp.Value
            }));

            globalByEventSourceEntries.AddRange(globalByEventSource.Select(kvp => new Contracts.Seeding.EventSourceSeedEntries
            {
                EventSourceId = kvp.Key.Value,
                Entries = kvp.Value
            }));
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
                    Tags = tags,
                    EventSourceType = entry.EventSourceType?.Value ?? EventSourceType.Default.Value,
                    EventStreamType = entry.EventStreamType?.Value ?? EventStreamType.All.Value,
                    EventStreamId = entry.EventStreamId?.Value ?? EventStreamId.Default
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

            namespaced.Add(new Contracts.Seeding.NamespacedSeedEntries
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

        await servicesAccessor.Services.Seeding.SeedEvents(new Contracts.Seeding.SeedEventsRequest
        {
            EventStore = _eventStoreName,
            GlobalByEventType = globalByEventTypeEntries,
            GlobalByEventSource = globalByEventSourceEntries,
            NamespacedEntries = namespaced
        }).EnsureSuccess();

        _entries.Clear();
    }

    /// <summary>
    /// Creates a new empty seeding buffer with the same event-store dependencies.
    /// </summary>
    /// <returns>A new <see cref="IEventSeeding"/> instance.</returns>
    internal IEventSeeding CreateEmpty() => new EventSeeding(
        _eventStoreName,
        _connection,
        _eventTypes,
        _eventSerializer,
        _clientArtifactsProvider,
        _serviceProvider,
        _artifactActivator,
        _logger,
        _eventSources);

    void AddRoutedEntries(IEnumerable<EventForEventSourceId> events, bool isGlobal, EventStoreNamespaceName targetNamespace, EventTypeId? explicitEventTypeId = default)
    {
        foreach (var entry in events)
        {
            var routing = entry.EventSource is not null
                ? ResolvedEventRouting.Resolve(_eventSources, entry.EventSource, entry.EventStream, entry.EventSourceType, entry.EventStreamType)
                : null;
            var eventTypeId = explicitEventTypeId ?? _eventTypes.GetEventTypeFor(entry.Event.GetType()).Id;
            var tags = entry.Event.GetType().GetTags().Concat(entry.Tags).Distinct().Select(tag => (Tag)tag);
            _entries.Add(new SeedingEntry(entry.EventSourceId, eventTypeId, entry.Event, tags, isGlobal, targetNamespace, routing?.SourceType ?? entry.EventSourceType, routing?.StreamType ?? entry.EventStreamType, entry.EventStreamId));
        }
    }

    void AddScopedEntry(EventSourceId eventSourceId, EventTypeId eventTypeId, object @event, IEnumerable<Tag> tags, bool isGlobal, EventStoreNamespaceName targetNamespace)
    {
        _entries.Add(new SeedingEntry(eventSourceId, eventTypeId, @event, tags, isGlobal, targetNamespace));
    }

    record SeedingEntry(
        EventSourceId EventSourceId,
        EventTypeId EventTypeId,
        object Event,
        IEnumerable<Tag> Tags,
        bool IsGlobal,
        EventStoreNamespaceName TargetNamespace,
        EventSourceType? EventSourceType = default,
        EventStreamType? EventStreamType = default,
        EventStreamId? EventStreamId = default);

    class EventSeedingScopeBuilder(EventSeeding parent, bool isGlobal, EventStoreNamespaceName targetNamespace) : IEventSeedingScopeBuilder
    {
        public IEventSeedingScopeBuilder For<TEvent>(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<TEvent> events, EventSourceType? eventSourceType = default)
            where TEvent : class
        {
            var routed = events.Select(@event => new EventForEventSourceId(eventSourceId, @event)
            {
                EventSourceType = eventSourceType ?? EventSourceType.Default,
                EventStreamType = eventStreamType,
                EventStreamId = eventStreamId
            });
            parent.AddRoutedEntries(routed, isGlobal, targetNamespace, parent._eventTypes.GetEventTypeFor(typeof(TEvent)).Id);
            return this;
        }

        public IEventSeedingScopeBuilder ForEventSource(EventSourceId eventSourceId, EventStreamType eventStreamType, EventStreamId eventStreamId, IEnumerable<object> events, EventSourceType? eventSourceType = default)
        {
            var routed = events.Select(@event => new EventForEventSourceId(eventSourceId, @event)
            {
                EventSourceType = eventSourceType ?? EventSourceType.Default,
                EventStreamType = eventStreamType,
                EventStreamId = eventStreamId
            });
            return ForEvents(routed);
        }

        public IEventSeedingScopeBuilder ForEvents(IEnumerable<EventForEventSourceId> events)
        {
            parent.AddRoutedEntries(events, isGlobal, targetNamespace);
            return this;
        }

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
