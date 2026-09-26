// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Testing.Compliance;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.Extensions.DependencyInjection;
using InMemoryClosedStreamsConstraintStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.ClosedStreamsConstraintStorage;
using InMemoryEventSequenceStorage = Cratis.Chronicle.Storage.InMemory.EventSequences.EventSequenceStorage;
using InMemoryIdentityStorage = Cratis.Chronicle.Storage.InMemory.Identities.IdentityStorage;
using InMemoryUniqueConstraintsStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.UniqueConstraintsStorage;
using InMemoryUniqueEventTypesConstraintsStorage = Cratis.Chronicle.Storage.InMemory.Events.Constraints.UniqueEventTypesConstraintsStorage;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IEventSequence"/> operations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// The internal implementation wires the real client <see cref="EventLog"/> to the real kernel
/// <c language="csharp">EventSequences</c> service backed by an <see cref="InProcessGrainFactory"/> that returns the
/// real kernel <c language="csharp">EventSequence</c> grain — no Orleans silo or Chronicle server required. Only the storage
/// layer is in-memory. Constraint validation, hash calculation, event serialization and event compliance
/// run through the actual kernel code paths. PII is protected in in-memory event storage and released on
/// read with scenario-local keys and generation-specific schemas. Erasure and production read-model
/// sink encryption are not supported by this scenario.
/// </para>
/// <para>
/// Use the <see cref="Given"/> property to seed pre-existing events into the event log before
/// exercising production code via <see cref="EventSequence"/> or <see cref="EventLog"/>.
/// </para>
/// <para>
/// Create a new <see cref="EventScenario"/> instance per test to keep tests isolated; the in-memory
/// event log accumulates state across calls on the same instance. Dispose the scenario when done to
/// release the in-process connection.
/// </para>
/// <para>
/// Usage:
/// <code language="csharp">
/// var scenario = new EventScenario();
/// await scenario.Given
///     .ForEventSource(myId)
///     .Events(new SomeEvent("value"), new OtherEvent("other"));
/// var result = await scenario.EventLog.Append(myId, new AnotherEvent("more"));
/// result.ShouldBeSuccessful();
/// </code>
/// </para>
/// </remarks>
/// <param name="eventSequenceId">The event sequence identifier.</param>
/// <param name="eventStoreName">The event store name.</param>
/// <param name="namespaceName">The event store namespace name.</param>
/// <param name="constraintProvider">The <see cref="ICanProvideConstraints"/> that supplies client-side constraint definitions. Pass <see langword="null"/> for no constraints.</param>
public class EventScenario(
    EventSequenceId eventSequenceId,
    EventStoreName eventStoreName,
    EventStoreNamespaceName namespaceName,
    ICanProvideConstraints? constraintProvider) : IDisposable
{
    readonly (EventLog EventLog, InProcessChronicleConnection Connection, InMemoryEventSequenceStorage Storage) _created = CreateEventLog(eventSequenceId, eventStoreName, namespaceName, constraintProvider);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class.
    /// </summary>
    /// <remarks>
    /// Constraints are automatically discovered from all loaded assemblies using the same discovery
    /// mechanism as the Chronicle client (<see cref="IConstraint"/> implementations, <c language="csharp">[Unique]</c>
    /// properties, and <c language="csharp">[UniqueEventType]</c> attributes).
    /// </remarks>
    public EventScenario()
        : this(
            EventSequenceId.Log,
            "test-event-store",
            "default",
            CreateDiscoveredConstraintProvider())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class with an explicit constraint provider.
    /// </summary>
    /// <param name="constraintProvider">The <see cref="ICanProvideConstraints"/> that supplies client-side constraint definitions. Pass <see langword="null"/> for no constraints.</param>
    public EventScenario(ICanProvideConstraints? constraintProvider)
        : this(
            EventSequenceId.Log,
            "test-event-store",
            "default",
            constraintProvider)
    {
    }

    /// <summary>
    /// Gets the fluent builder used to seed pre-existing events into the event log before the act phase.
    /// </summary>
    public EventScenarioGivenBuilder Given => new(_created.EventLog);

    /// <summary>
    /// Gets the fluent builder used to append the event(s) under test during the act phase and return the resulting <see cref="AppendResult"/>.
    /// </summary>
    /// <remarks>
    /// Symmetric to <see cref="Given"/>: where <c language="csharp">Given</c> seeds pre-existing events, <c language="csharp">When</c> performs the act being
    /// tested. Its terminal <see cref="EventSourceWhenBuilder.Events"/> returns the <see cref="AppendResult"/> — the same
    /// "the act returns its result" shape as <c language="csharp">CommandScenario.Execute</c>, so constraint/append specs read as
    /// Given / When / then without binding the raw <see cref="IEventSequence.Append"/> overload by hand.
    /// <code language="csharp">
    /// await scenario.Given.ForEventSource(id).Events(seedEvent);
    /// var result = await scenario.When.ForEventSource(id).Events(actEvent);
    /// result.ShouldHaveConstraintViolationFor(name);
    /// </code>
    /// </remarks>
    public EventScenarioWhenBuilder When => new(_created.EventLog);

    /// <summary>
    /// Gets the <see cref="IEventLog"/> backed by the real kernel event sequence grain via the real client event log.
    /// </summary>
    public IEventLog EventLog => _created.EventLog;

    /// <summary>
    /// Gets the <see cref="IEventSequence"/> backed by the real kernel event sequence grain via the real client event log.
    /// </summary>
    /// <remarks>
    /// This is the same underlying instance as <see cref="EventLog"/>.
    /// </remarks>
    public IEventSequence EventSequence => _created.EventLog;

    /// <inheritdoc/>
    public void Dispose() => _created.Connection.Dispose();

    /// <summary>
    /// Reads stored event JSON before compliance release, for the harness's own regression specs only.
    /// </summary>
    /// <param name="sequenceNumber">The stored event position.</param>
    /// <returns>The protected event content.</returns>
    internal async Task<string> ReadContentAtRest(EventSequenceNumber sequenceNumber)
    {
        var stored = await _created.Storage.GetEventAt((KernelConceptsNs::Events.EventSequenceNumber)sequenceNumber.Value);
        return JsonSerializer.Serialize(stored.Content);
    }

    static (EventLog EventLog, InProcessChronicleConnection Connection, InMemoryEventSequenceStorage Storage) CreateEventLog(
        EventSequenceId eventSequenceId,
        EventStoreName eventStoreName,
        EventStoreNamespaceName namespaceName,
        ICanProvideConstraints? constraintProvider)
    {
        var defaults = Defaults.Instance;
        var compliance = new InProcessCompliance();
        var kernelEventSequenceId = (KernelSequenceConcepts::EventSequenceId)(string)eventSequenceId;
        var kernelEventStoreName = (KernelConceptsNs::EventStoreName)(string)eventStoreName;
        var kernelNamespaceName = (KernelConceptsNs::EventStoreNamespaceName)(string)namespaceName;

        var identityStorage = new InMemoryIdentityStorage();
        var eventSequenceStorage = new InMemoryEventSequenceStorage(kernelEventStoreName, kernelNamespaceName, kernelEventSequenceId, identityStorage);
        var uniqueConstraintsStorage = new InMemoryUniqueConstraintsStorage();
        var uniqueEventTypesStorage = new InMemoryUniqueEventTypesConstraintsStorage(eventSequenceStorage);
        var closedStreamsStorage = new InMemoryClosedStreamsConstraintStorage();
        var resolvedConstraintProvider = constraintProvider ?? new EmptyConstraintProvider();
        var constraintsStorage = new InMemoryConstraintsStorage(resolvedConstraintProvider);
        var eventTypesStorage = new InMemoryEventTypesStorage(() => defaults.EventTypes, defaults.JsonSchemaGenerator);

        var storage = new InMemoryStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            closedStreamsStorage,
            identityStorage,
            eventTypesStorage);

        var grain = InProcessEventSequence.Create(
            storage,
            kernelEventSequenceId,
            kernelEventStoreName,
            kernelNamespaceName,
            compliance).GetAwaiter().GetResult();

        var grainFactory = new InProcessGrainFactory(grain);

        var jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        var eventCompliance = compliance.CreateEventCompliance();
        var sequencesService = new KernelGrpc::Cratis.Chronicle.Services.Sequences.EventSequences(
            InProcessCommandPipeline.Create(
                grainFactory,
                storage,
                jsonSerializerOptions,
                services =>
                {
                    services.AddSingleton<IUnitOfWorkManager>(new NoOpUnitOfWorkManager());
                    services.AddSingleton<IEventLog>(new NoOpEventLog());
                    services.AddSingleton(defaults.EventTypes);
                    services.AddSingleton<KernelCore::Cratis.Chronicle.Events.IEventCompliance>(eventCompliance);
                }),
            storage,
            eventCompliance,
            jsonSerializerOptions,
            new InProcessQueryContextManager(),
            grainFactory,
            NullLogger<KernelGrpc::Cratis.Chronicle.Services.Sequences.EventSequences>.Instance);

        var constraintsService = new InProcessNoOpConstraintsService();
        var services = new InProcessServices(sequencesService, constraintsService);
        var connection = new InProcessChronicleConnection(services);

        var inProcessConstraints = new InProcessConstraints(resolvedConstraintProvider);
        inProcessConstraints.Discover().GetAwaiter().GetResult();

        var eventLog = new EventLog(
            eventStoreName,
            namespaceName,
            connection,
            defaults.EventTypes,
            inProcessConstraints,
            defaults.EventSerializer,
            new CorrelationIdAccessor(),
            new NoConcurrencyScopeStrategies(),
            new CausationManager(),
            new NoUnitOfWorkManager(),
            new BaseIdentityProvider(),
            jsonSerializerOptions);

        return (eventLog, connection, eventSequenceStorage);
    }

    static CompositeConstraintProvider CreateDiscoveredConstraintProvider()
    {
        var defaults = Defaults.Instance;
        var namingPolicy = new CamelCaseNamingPolicy();
        using var serviceProvider = new DefaultServiceProvider();
        using var loggerFactory = new NullLoggerFactory();
        var artifactActivator = new ClientArtifactsActivator(serviceProvider, loggerFactory);
        return new CompositeConstraintProvider(
            new ConstraintsByBuilderProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes,
                namingPolicy,
                artifactActivator,
                NullLogger<ConstraintsByBuilderProvider>.Instance),
            new UniqueConstraintProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes,
                namingPolicy),
            new UniqueEventTypeConstraintsProvider(
                defaults.ClientArtifactsProvider,
                defaults.EventTypes));
    }

    sealed class CompositeConstraintProvider(params ICanProvideConstraints[] providers) : ICanProvideConstraints
    {
        /// <inheritdoc/>
        public IImmutableList<IConstraintDefinition> Provide() =>
            providers
                .SelectMany(p => p.Provide())
                .ToImmutableList();
    }

    sealed class EmptyConstraintProvider() : ICanProvideConstraints
    {
        /// <inheritdoc/>
        public IImmutableList<IConstraintDefinition> Provide() => ImmutableList<IConstraintDefinition>.Empty;
    }

    sealed class NoConcurrencyScopeStrategies : IConcurrencyScopeStrategies
    {
        /// <inheritdoc/>
        public IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence) => NoConcurrencyScopeStrategy.Instance;
    }

    sealed class NoConcurrencyScopeStrategy : IConcurrencyScopeStrategy
    {
        internal static readonly NoConcurrencyScopeStrategy Instance = new();

        /// <inheritdoc/>
        public Task<ConcurrencyScope> GetScope(
            EventSourceId eventSourceId,
            EventStreamType? eventStreamType = default,
            EventStreamId? eventStreamId = default,
            EventSourceType? eventSourceType = default,
            IEnumerable<EventType>? eventTypes = default) =>
            Task.FromResult(ConcurrencyScope.None);
    }

    sealed class NoUnitOfWorkManager : IUnitOfWorkManager
    {
        /// <inheritdoc/>
        public IUnitOfWork Current => throw new NoUnitOfWorkHasBeenStarted();

        /// <inheritdoc/>
        public bool HasCurrent => false;

        /// <inheritdoc/>
        public bool TryGetFor(CorrelationId correlationId, [MaybeNullWhen(false)] out IUnitOfWork unitOfWork)
        {
            unitOfWork = null;
            return false;
        }

        /// <inheritdoc/>
        public IUnitOfWork Begin(CorrelationId correlationId) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");

        /// <inheritdoc/>
        public void SetCurrent(IUnitOfWork unitOfWork) => throw new NotSupportedException("Unit of work is not supported in test scenarios.");
    }
}
