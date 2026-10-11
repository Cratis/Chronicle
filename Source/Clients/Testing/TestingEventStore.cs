// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Events.Migrations;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.InMemory.Events.EventTypes;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Testing.Compliance;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage;
using Cratis.Serialization;
using Cratis.Types;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Owns the production in-memory storage and in-process kernel collaborators of one test store.
/// </summary>
internal sealed class TestingEventStore : IDisposable
{
    readonly ConcurrentDictionary<EventSequenceId, Lazy<KernelSequences::IEventSequence>> _grains = new();
    readonly EventStoreStorages _storages;
    readonly Func<IEventTypes> _eventTypes;
    readonly JsonSerializerOptions _json = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
    readonly InProcessGrainFactory _grainFactory;
    bool _disposed;

    /// <summary>
    /// Initializes an isolated in-process event store.
    /// </summary>
    /// <param name="name">The event store name.</param>
    /// <param name="ns">The namespace name.</param>
    /// <param name="eventTypes">The discovered event types.</param>
    internal TestingEventStore(EventStoreName name, EventStoreNamespaceName ns, Func<IEventTypes> eventTypes)
    {
        Name = name;
        Namespace = ns;
        _eventTypes = eventTypes;
        _storages = new EventStoreStorages(
            new KnownInstancesOf<ISinkFactory>([new InMemorySinkFactory(new TypeFormats())]),
            new InMemoryJobsStorage(new NoJobTypes()),
            _ => new DiscoveredEventTypesStorage(new EventTypesStorage(), eventTypes));
        Storage = new Storage.InMemory.Storage(_storages, new SystemStorage());
        _grainFactory = new InProcessGrainFactory(sequenceResolver: ResolveSequence);
        Connection = new ChronicleConnectionForTesting(_grainFactory, Storage, Compliance, _json, eventTypes, Dispose);
    }

    /// <summary>
    /// Gets the event store name.
    /// </summary>
    internal EventStoreName Name { get; }

    /// <summary>
    /// Gets the namespace name.
    /// </summary>
    internal EventStoreNamespaceName Namespace { get; }

    /// <summary>
    /// Gets the production storage.
    /// </summary>
    internal IStorage Storage { get; }

    /// <summary>
    /// Gets the shared compliance stack for internal harness specifications.
    /// </summary>
    internal InProcessCompliance Compliance { get; } = new();

    /// <summary>
    /// Gets the connection sharing the storage and command pipeline.
    /// </summary>
    internal ChronicleConnectionForTesting Connection { get; }

    /// <summary>
    /// Gets storage for this event store.
    /// </summary>
    internal IEventStoreStorage Store => Storage.GetEventStore(new KernelConceptsNs::EventStoreName(Name.Value));

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var grain in _grains.Values.Where(grain => grain.IsValueCreated))
        {
            ((IDisposable)grain.Value).Dispose();
        }
        _storages.Dispose();
    }

    /// <summary>
    /// Discovers all client constraint declarations, including closed streams.
    /// </summary>
    /// <param name="artifacts">The client artifacts.</param>
    /// <param name="types">The discovered event types.</param>
    /// <param name="activator">The artifact activator.</param>
    /// <returns>The constraint provider.</returns>
    internal static ICanProvideConstraints DiscoverConstraints(IClientArtifactsProvider artifacts, IEventTypes types, IClientArtifactsActivator activator)
    {
        var naming = new CamelCaseNamingPolicy();
        return new CompositeConstraintProvider(
            new ConstraintsByBuilderProvider(artifacts, types, naming, activator, NullLogger<ConstraintsByBuilderProvider>.Instance),
            new UniqueConstraintProvider(artifacts, types, naming),
            new UniqueEventTypeConstraintsProvider(artifacts, types),
            new ClosesStreamConstraintsProvider(artifacts, types, naming));
    }

    /// <summary>
    /// Seeds definitions before any grain creates its validators.
    /// </summary>
    /// <param name="constraints">Constraints to seed; null means none.</param>
    /// <param name="schemas">The schema generator.</param>
    /// <param name="migrators">The event migrators.</param>
    /// <param name="eventSources">The discovered event sources.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task Seed(ICanProvideConstraints? constraints, IJsonSchemaGenerator schemas, IEventTypeMigrators migrators, IEnumerable<EventSources.EventSourceDefinition> eventSources)
    {
        foreach (var definition in ClosesStreamDeclarations.Merge(constraints?.Provide() ?? []))
        {
            await Store.Constraints.SaveDefinition(KernelGrpc::Cratis.Chronicle.Services.Events.Constraints.ConstraintConverters.ToChronicle(definition.ToContract()));
        }
        await TestingEventTypes.Seed(Store.EventTypes, _eventTypes(), schemas, migrators);
        foreach (var definition in eventSources)
        {
            await Store.EventSources.Save(definition.ToKernel());
        }
    }

    /// <summary>
    /// Creates a client sequence sharing the store's connection and storage.
    /// </summary>
    /// <param name="id">The sequence identifier.</param>
    /// <param name="serializer">The event serializer.</param>
    /// <param name="constraints">Client constraints.</param>
    /// <param name="eventSources">Client event sources.</param>
    /// <returns>The client sequence.</returns>
    internal IEventSequence CreateSequence(EventSequenceId id, IEventSerializer serializer, IConstraints constraints, IEventSources eventSources)
    {
        if (id == EventSequenceId.Log)
        {
            return new EventLog(Name, Namespace, Connection, _eventTypes(), constraints, serializer, new CorrelationIdAccessor(), new NoConcurrencyScopeStrategies(), new CausationManager(), new NoUnitOfWorkManager(), new BaseIdentityProvider(), _json, eventSources: eventSources);
        }
        return new EventSequence(Name, Namespace, id, Connection, _eventTypes(), constraints, serializer, new CorrelationIdAccessor(), new NoConcurrencyScopeStrategies(), new CausationManager(), new NoUnitOfWorkManager(), new BaseIdentityProvider(), _json, eventSources: eventSources);
    }

    KernelSequences::IEventSequence ResolveSequence(string key)
    {
        var parsed = KernelConceptsNs::EventSequences.EventSequenceKey.Parse(key);
        if (parsed.EventStore.Value != Name.Value || parsed.Namespace.Value != Namespace.Value || parsed.EventSequenceId == KernelConceptsNs::EventSequences.EventSequenceId.System)
        {
            throw new GrainNotAvailableInTestScenario($"Event sequence '{key}'");
        }
        return _grains.GetOrAdd(
            new EventSequenceId(parsed.EventSequenceId.Value),
            static (id, root) => new Lazy<KernelSequences::IEventSequence>(() =>
                SerializedEventSequence.Create(InProcessEventSequence.Create(root.Storage, new(id.Value), new(root.Name.Value), new(root.Namespace.Value), root.Compliance)
                    .GetAwaiter().GetResult())),
            this).Value;
    }

    sealed class CompositeConstraintProvider(params ICanProvideConstraints[] providers) : ICanProvideConstraints
    {
        public IImmutableList<IConstraintDefinition> Provide() => providers.SelectMany(provider => provider.Provide()).ToImmutableList();
    }

    sealed class NoJobTypes : IJobTypes
    {
        public IEnumerable<JobType> All => [];
        public Result<JobType, IJobTypes.GetForError> GetFor(Type type) => IJobTypes.GetForError.NoAssociatedJobType;
        public Result<Type, IJobTypes.GetClrTypeForError> GetClrTypeFor(JobType type) => IJobTypes.GetClrTypeForError.CouldNotFindType;
        public Result<Type, IJobTypes.GetRequestClrTypeForError> GetRequestClrTypeFor(JobType type) => IJobTypes.GetRequestClrTypeForError.CouldNotFindType;
    }

    sealed class NoConcurrencyScopeStrategies : IConcurrencyScopeStrategies
    {
        public IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence) => new NoConcurrencyScopeStrategy();
    }

    sealed class NoConcurrencyScopeStrategy : IConcurrencyScopeStrategy
    {
        public Task<ConcurrencyScope> GetScope(EventSourceId eventSourceId, EventStreamType? eventStreamType = default, EventStreamId? eventStreamId = default, EventSourceType? eventSourceType = default, IEnumerable<EventType>? eventTypes = default) => Task.FromResult(ConcurrencyScope.None);
    }

    sealed class NoUnitOfWorkManager : IUnitOfWorkManager
    {
        public IUnitOfWork Current => throw new NoUnitOfWorkHasBeenStarted();
        public bool HasCurrent => false;
        public bool TryGetFor(CorrelationId correlationId, [MaybeNullWhen(false)] out IUnitOfWork unitOfWork)
        {
            unitOfWork = null;
            return false;
        }
        public IUnitOfWork Begin(CorrelationId correlationId) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");
        public void SetCurrent(IUnitOfWork unitOfWork) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");
    }
}
