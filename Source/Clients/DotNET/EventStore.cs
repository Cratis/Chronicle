// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventStoreSubscriptions;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Seeding;
using Cratis.Chronicle.Transactions;
using Cratis.Chronicle.Webhooks;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly EventStoreName _eventStoreName;
    readonly IChronicleServicesAccessor _servicesAccessor;
    readonly ICorrelationIdAccessor _correlationIdAccessor;
    readonly IConcurrencyScopeStrategies _concurrencyScopeStrategies;
    readonly ICausationManager _causationManager;
    readonly IIdentityProvider _identityProvider;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IEventSerializer _eventSerializer;
    readonly IClientArtifactsProvider _clientArtifactsProvider;
    readonly ILogger<EventStore> _logger;
    readonly ConcurrentDictionary<EventSequenceId, IEventSequence> _sequences = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="eventStoreName">Name of the event store.</param>
    /// <param name="namespace">Namespace for the event store.</param>
    /// <param name="connection"><see cref="IChronicleConnection"/> for working with the connection to Chronicle.</param>
    /// <param name="clientArtifactsProvider"><see cref="IClientArtifactsProvider"/> for getting client artifacts.</param>
    /// <param name="eventTypeMigrators"><see cref="IEventTypeMigrators"/> for getting event type migrators.</param>
    /// <param name="correlationIdAccessor"><see cref="ICorrelationIdAccessor"/> for getting correlation.</param>
    /// <param name="concurrencyScopeStrategies"><see cref="IConcurrencyScopeStrategies"/> for managing concurrency scopes.</param>
    /// <param name="causationManager"><see cref="ICausationManager"/> for getting causation.</param>
    /// <param name="identityProvider"><see cref="IIdentityProvider"/> for resolving identity for operations.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating JSON schemas.</param>
    /// <param name="namingPolicy"><see cref="INamingPolicy"/> to use for converting names during serialization.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting instances of services.</param>
    /// <param name="artifactActivator"><see cref="IClientArtifactsActivator"/> for creating artifact instances.</param>
    /// <param name="autoDiscoverAndRegister">Whether to automatically discover and register artifacts.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="enableEventTypeGenerationValidation">Whether to enable event type generation chain validation. Defaults to <see langword="false"/>.</param>
    /// <param name="options">The <see cref="IOptions{ChronicleOptions}"/> for Chronicle configuration.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStore(
        EventStoreName eventStoreName,
        EventStoreNamespaceName @namespace,
        IChronicleConnection connection,
        IClientArtifactsProvider clientArtifactsProvider,
        IEventTypeMigrators eventTypeMigrators,
        ICorrelationIdAccessor correlationIdAccessor,
        IConcurrencyScopeStrategies concurrencyScopeStrategies,
        ICausationManager causationManager,
        IIdentityProvider identityProvider,
        IJsonSchemaGenerator schemaGenerator,
        INamingPolicy namingPolicy,
        IServiceProvider serviceProvider,
        IClientArtifactsActivator artifactActivator,
        bool autoDiscoverAndRegister,
        JsonSerializerOptions jsonSerializerOptions,
        bool enableEventTypeGenerationValidation,
        IOptions<ChronicleOptions> options,
        ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<EventStore>();
        _eventStoreName = eventStoreName;
        _causationManager = causationManager;
        _identityProvider = identityProvider;
        _jsonSerializerOptions = jsonSerializerOptions;
        _clientArtifactsProvider = clientArtifactsProvider;
        Name = eventStoreName;
        Namespace = @namespace;
        Connection = connection;
        _servicesAccessor = (connection as IChronicleServicesAccessor)!;
        _correlationIdAccessor = correlationIdAccessor;
        _concurrencyScopeStrategies = concurrencyScopeStrategies;
        EventTypes = new EventTypes(this, schemaGenerator, clientArtifactsProvider, eventTypeMigrators, enableEventTypeGenerationValidation);
        UnitOfWorkManager = new UnitOfWorkManager(this);
        _correlationIdAccessor = correlationIdAccessor;

        _eventSerializer = new EventSerializer(
            clientArtifactsProvider,
            artifactActivator,
            EventTypes,
            jsonSerializerOptions);

        Constraints = new Constraints(
            this,
            [
                new ConstraintsByBuilderProvider(clientArtifactsProvider, EventTypes, namingPolicy, artifactActivator, loggerFactory.CreateLogger<ConstraintsByBuilderProvider>()),
                new UniqueConstraintProvider(clientArtifactsProvider, EventTypes, namingPolicy),
                new UniqueEventTypeConstraintsProvider(clientArtifactsProvider, EventTypes)
            ]);

        EventLog = new EventLog(
            eventStoreName,
            @namespace,
            connection,
            EventTypes,
            Constraints,
            _eventSerializer,
            correlationIdAccessor,
            concurrencyScopeStrategies,
            causationManager,
            UnitOfWorkManager,
            identityProvider,
            jsonSerializerOptions);
        _sequences[EventLog.Id] = EventLog;

        Jobs = new Jobs.Jobs(this);

        Reactors = new Reactors.Reactors(
            this,
            EventTypes,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            new ReactorMiddlewaresActivator(clientArtifactsProvider, artifactActivator, loggerFactory.CreateLogger<ReactorMiddlewaresActivator>()),
            _eventSerializer,
            causationManager,
            identityProvider,
            loggerFactory.CreateLogger<Reactors.Reactors>(),
            loggerFactory);

        var reducerObservers = new ReducerObservers();

        Reducers = new Reducers.Reducers(
            this,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            new ReducerValidator(),
            EventTypes,
            namingPolicy,
            jsonSerializerOptions,
            options,
            identityProvider,
            reducerObservers,
            loggerFactory.CreateLogger<Reducers.Reducers>());

        var projections = new Projections.Projections(
            this,
            EventTypes,
            clientArtifactsProvider,
            namingPolicy,
            artifactActivator,
            jsonSerializerOptions,
            loggerFactory.CreateLogger<Projections.Projections>());

        Projections = projections;
        Webhooks = new Webhooks.Webhooks(EventTypes, this, loggerFactory.CreateLogger<Webhooks.Webhooks>());
        Subscriptions = new EventStoreSubscriptions.EventStoreSubscriptions(EventTypes, this, loggerFactory.CreateLogger<EventStoreSubscriptions.EventStoreSubscriptions>());
        FailedPartitions = new FailedPartitions(this);

        var readModelsWatcherManager = new ReadModelWatcherManager(new ReadModelWatcherFactory(this, jsonSerializerOptions));

        ReadModels = new ReadModels.ReadModels(
            this,
            namingPolicy,
            projections,
            Reducers,
            EventTypes,
            schemaGenerator,
            options,
            jsonSerializerOptions,
            readModelsWatcherManager,
            reducerObservers,
            loggerFactory.CreateLogger<ReadModels.ReadModels>());

        Seeding = new EventSeeding(
            eventStoreName,
            connection,
            EventTypes,
            _eventSerializer,
            clientArtifactsProvider,
            serviceProvider,
            artifactActivator,
            loggerFactory.CreateLogger<EventSeeding>());

        if (autoDiscoverAndRegister)
        {
            Connection.Lifecycle.OnConnected += RegisterAll;
        }
    }

    /// <inheritdoc/>
    public EventStoreName Name { get; }

    /// <inheritdoc/>
    public EventStoreNamespaceName Namespace { get; }

    /// <inheritdoc/>
    public IChronicleConnection Connection { get; }

    /// <inheritdoc/>
    public IUnitOfWorkManager UnitOfWorkManager { get; }

    /// <inheritdoc/>
    public IEventTypes EventTypes { get; }

    /// <inheritdoc/>
    public IConstraints Constraints { get; }

    /// <inheritdoc/>
    public IEventLog EventLog { get; }

    /// <inheritdoc/>
    public IJobs Jobs { get; }

    /// <inheritdoc/>
    public IReactors Reactors { get; }

    /// <inheritdoc/>
    public IReducers Reducers { get; }

    /// <inheritdoc/>
    public IProjections Projections { get; }

    /// <inheritdoc/>
    public IWebhooks Webhooks { get; }

    /// <inheritdoc/>
    public IEventStoreSubscriptions Subscriptions { get; }

    /// <inheritdoc/>
    public IFailedPartitions FailedPartitions { get; }

    /// <inheritdoc/>
    public IReadModels ReadModels { get; }

    /// <inheritdoc/>
    public IEventSeeding Seeding { get; }

    /// <inheritdoc/>
    public async Task DiscoverAll()
    {
        _logger.DiscoverAllArtifacts();

        // We need to discover all event types first, as they are used by the other artifacts
        await EventTypes.Discover();

        await Task.WhenAll(
            Constraints.Discover(),
            Reactors.Discover(),
            Reducers.Discover(),
            Projections.Discover(),
            Seeding.Discover());
    }

    /// <inheritdoc/>
    public async Task RegisterAll()
    {
        _logger.RegisterAllArtifacts();

        // Ensure the event store exists before registering artifacts.
        await _servicesAccessor.Services.EventStores.Ensure(new EnsureEventStore { Name = Name.Value });

        // We need to register event types and read models first, as they are used by the other artifacts
        await Task.WhenAll(
            EventTypes.Register(),
            ReadModels.Register());

        // Register all observers before seeding to prevent race conditions where
        // seeded events arrive at the kernel before observers are registered
        await Task.WhenAll(
            Constraints.Register(),
            Reactors.Register(),
            Reducers.Register(),
            Projections.Register());

        // Auto-subscribe to any external event stores referenced by observers
        await RegisterExternalEventStoreSubscriptionsAsync();

        // Seed events only after all observers are registered
        await Seeding.Register();
    }

    /// <inheritdoc/>
    public IEventSequence GetEventSequence(EventSequenceId id) =>
        _sequences.GetOrAdd(
            id,
            static (key, state) => new EventSequence(
                state._eventStoreName,
                state.Namespace,
                key,
                state.Connection,
                state.EventTypes,
                state.Constraints,
                state._eventSerializer,
                state._correlationIdAccessor,
                state._concurrencyScopeStrategies,
                state._causationManager,
                state.UnitOfWorkManager,
                state._identityProvider,
                state._jsonSerializerOptions),
            this);

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default)
    {
        var namespaces = await _servicesAccessor.Services.Namespaces.GetNamespaces(new() { EventStore = _eventStoreName });
        return namespaces.Select(_ => (EventStoreNamespaceName)_).ToArray();
    }

    async Task RegisterExternalEventStoreSubscriptionsAsync()
    {
        var currentStoreName = _eventStoreName?.Value;

        static string? ResolveSourceStoreForObserver(Type observerType)
        {
            var observerEventStore = observerType.GetCustomAttributes(typeof(EventStoreAttribute), inherit: true)
                .OfType<EventStoreAttribute>()
                .FirstOrDefault();
            if (observerEventStore is not null)
            {
                return observerEventStore.EventStore;
            }

            var eventStores = observerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
                .OfType<Type>()
                .Where(t => Attribute.IsDefined(t, typeof(EventTypeAttribute), inherit: true))
                .Select(t => t.GetEventStoreName())
                .Where(name => name is not null)
                .Select(name => name!)
                .Distinct()
                .ToList();

            if (eventStores.Count == 1)
            {
                return eventStores[0];
            }

            return null;
        }

        static IEnumerable<EventTypeId> ResolveObserverEventTypeIds(Type observerType, IEventTypes eventTypes) =>
            observerType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => !m.IsSpecialName)
                .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
                .OfType<Type>()
                .Where(t => Attribute.IsDefined(t, typeof(EventTypeAttribute), inherit: true))
                .Select(t => eventTypes.GetEventTypeFor(t).Id)
                .Distinct()
                .ToArray();

        var observerExternalSubscriptions = _clientArtifactsProvider.Reactors
            .Where(r => !ReactorTypeExtensions.HasExplicitEventSequence(r))
            .Concat(_clientArtifactsProvider.Reducers.Where(r => !ReducerTypeExtensions.HasExplicitEventSequence(r)))
            .Select(observerType =>
            {
                var sourceStore = ResolveSourceStoreForObserver(observerType);
                return (observerType, sourceStore);
            })
            .Where(item => item.sourceStore is not null && item.sourceStore != currentStoreName)
            .GroupBy(item => item.sourceStore!, item => item.observerType)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(observerType => ResolveObserverEventTypeIds(observerType, EventTypes)).Distinct().ToList());

        // Merge with projection external event store references (already filtered by GetExternalEventStoreSubscriptions)
        var allByStore = new Dictionary<string, HashSet<EventTypeId>>();
        foreach (var kvp in observerExternalSubscriptions)
        {
            allByStore[kvp.Key] = [.. kvp.Value];
        }

        foreach (var subscription in Projections.GetExternalEventStoreSubscriptions())
        {
            if (!allByStore.TryGetValue(subscription.EventStoreName, out var existing))
            {
                existing = [];
                allByStore[subscription.EventStoreName] = existing;
            }

            foreach (var id in subscription.EventTypeIds)
            {
                existing.Add(id);
            }
        }

        // Register a subscription for each external event store
        foreach (var kvp in allByStore)
        {
            await Subscriptions.Subscribe(
                new EventStoreSubscriptionId(kvp.Key),
                kvp.Key,
                builder =>
                {
                    foreach (var id in kvp.Value)
                    {
                        builder.WithEventType(id);
                    }
                });
        }
    }
}
