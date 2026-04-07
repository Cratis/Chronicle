// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Json;
using Cratis.Serialization;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IEventSequence"/> operations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// The internal implementation wires the real client <see cref="EventLog"/> to an in-process
/// <see cref="InProcessEventSequencesService"/> that delegates all append operations to the real
/// kernel <c>EventSequence</c> grain — no Orleans silo or Chronicle server required. Only the storage
/// layer is in-memory. All business logic (constraint validation, hash calculation, event serialization,
/// migration, compliance) runs through the actual kernel code paths.
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
/// <code>
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
    readonly (EventLog EventLog, InProcessChronicleConnection Connection) _created = CreateEventLog(eventSequenceId, eventStoreName, namespaceName, constraintProvider);

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class.
    /// </summary>
    /// <remarks>
    /// Constraints are automatically discovered from all loaded assemblies using the same discovery
    /// mechanism as the Chronicle client (<see cref="IConstraint"/> implementations, <c>[Unique]</c>
    /// properties, and <c>[UniqueEventType]</c> attributes).
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

    static (EventLog EventLog, InProcessChronicleConnection Connection) CreateEventLog(
        EventSequenceId eventSequenceId,
        EventStoreName eventStoreName,
        EventStoreNamespaceName namespaceName,
        ICanProvideConstraints? constraintProvider)
    {
        var kernelEventSequenceId = (KernelSequenceConcepts::EventSequenceId)(string)eventSequenceId;
        var kernelEventStoreName = (KernelConceptsNs::EventStoreName)(string)eventStoreName;
        var kernelNamespaceName = (KernelConceptsNs::EventStoreNamespaceName)(string)namespaceName;

        var eventSequenceStorage = new InMemoryEventSequenceStorage(kernelEventSequenceId);
        var uniqueConstraintsStorage = new InMemoryUniqueConstraintsStorage();
        var uniqueEventTypesStorage = new InMemoryUniqueEventTypesConstraintsStorage(eventSequenceStorage);
        var resolvedConstraintProvider = constraintProvider ?? new EmptyConstraintProvider();
        var constraintsStorage = new InMemoryConstraintsStorage(resolvedConstraintProvider);
        var identityStorage = new InMemoryIdentityStorage();
        var eventTypesStorage = new InMemoryEventTypesStorage();

        var storage = new InMemoryKernelStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            identityStorage,
            eventTypesStorage);

        var grain = InProcessEventSequence.Create(
            storage,
            kernelEventSequenceId,
            kernelEventStoreName,
            kernelNamespaceName).GetAwaiter().GetResult();

        var grainFactory = new InProcessGrainFactory(grain);

        var jsonSerializerOptions = Globals.JsonSerializerOptions ?? new JsonSerializerOptions();
        var eventSequencesService = new InProcessEventSequencesService(
            grainFactory,
            jsonSerializerOptions);

        var constraintsService = new InProcessNoOpConstraintsService();
        var services = new InProcessServices(eventSequencesService, constraintsService);
        var connection = new InProcessChronicleConnection(services);

        var defaults = Defaults.Instance;
        var inProcessConstraints = new InProcessConstraints(resolvedConstraintProvider);
        inProcessConstraints.Discover().GetAwaiter().GetResult();

        var eventLog = new EventLog(
            eventStoreName,
            namespaceName,
            connection,
            defaults.EventTypes,
            inProcessConstraints,
            defaults.EventSerializer,
            new Execution.CorrelationIdAccessor(),
            new NoConcurrencyScopeStrategies(),
            new CausationManager(),
            new NoUnitOfWorkManager(),
            new BaseIdentityProvider(),
            jsonSerializerOptions);

        return (eventLog, connection);
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
