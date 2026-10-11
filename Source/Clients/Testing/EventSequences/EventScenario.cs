// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an isolated, in-process scenario backed by the real kernel append path and production in-memory storage.
/// </summary>
/// <remarks>
/// Create a new scenario per specification. Events start at sequence number zero and accumulate on that instance.
/// Constraints, serialization, migrations and PII compliance use the kernel implementations; observers do not run.
/// Waiting for observer completion throws <see cref="Observation.CannotWaitForObserverCompletion"/>.
/// PII keys are scenario-local. Erasure and production read-model sink encryption are not supported.
/// </remarks>
/// <param name="eventSequenceId">The event sequence identifier.</param>
/// <param name="eventStoreName">The event store name.</param>
/// <param name="namespaceName">The namespace name.</param>
/// <param name="constraintProvider">The constraint provider; null means no constraints.</param>
/// <param name="defaults">Defaults for artifact discovery and serialization.</param>
public class EventScenario(
    EventSequenceId eventSequenceId,
    EventStoreName eventStoreName,
    EventStoreNamespaceName namespaceName,
    ICanProvideConstraints? constraintProvider,
    Defaults defaults) : IDisposable
{
    readonly (IEventLog Log, TestingEventStore Root) _created = Create(eventSequenceId, eventStoreName, namespaceName, constraintProvider, defaults);

    /// <summary>
    /// Initializes a scenario with constraints discovered from the loaded assemblies.
    /// </summary>
    public EventScenario() : this(EventSequenceId.Log, "test-event-store", "default")
    {
    }

    /// <summary>
    /// Initializes a scenario with an explicit constraint provider.
    /// </summary>
    /// <param name="constraintProvider">The constraint provider; null means no constraints.</param>
    public EventScenario(ICanProvideConstraints? constraintProvider)
        : this(EventSequenceId.Log, "test-event-store", "default", constraintProvider, Defaults.Instance)
    {
    }

    /// <summary>
    /// Initializes a scenario with per-run defaults.
    /// </summary>
    /// <param name="defaults">Defaults for artifact discovery and serialization.</param>
    /// <param name="constraintProvider">Null discovers constraints from the defaults; an empty provider disables them.</param>
    public EventScenario(Defaults defaults, ICanProvideConstraints? constraintProvider = null)
        : this(EventSequenceId.Log, "test-event-store", "default", constraintProvider ?? DiscoverConstraints(defaults), defaults)
    {
    }

    /// <summary>
    /// Initializes a scenario with explicit identifiers and discovered constraints.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    public EventScenario(EventSequenceId eventSequenceId, EventStoreName eventStoreName, EventStoreNamespaceName namespaceName)
        : this(Defaults.Instance, eventSequenceId, eventStoreName, namespaceName)
    {
    }

    /// <summary>
    /// Initializes a scenario with explicit identifiers and constraints discovered from per-run defaults.
    /// </summary>
    /// <param name="defaults">Defaults for artifact discovery and serialization.</param>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    public EventScenario(Defaults defaults, EventSequenceId eventSequenceId, EventStoreName eventStoreName, EventStoreNamespaceName namespaceName)
        : this(eventSequenceId, eventStoreName, namespaceName, DiscoverConstraints(defaults), defaults)
    {
    }

    /// <summary>
    /// Initializes a scenario with explicit identifiers and an explicit constraint provider.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="constraintProvider">The constraint provider; null means no constraints.</param>
    public EventScenario(EventSequenceId eventSequenceId, EventStoreName eventStoreName, EventStoreNamespaceName namespaceName, ICanProvideConstraints? constraintProvider)
        : this(eventSequenceId, eventStoreName, namespaceName, constraintProvider, Defaults.Instance)
    {
    }

    /// <summary>
    /// Gets the builder for seeding events before the act phase.
    /// </summary>
    public EventScenarioGivenBuilder Given => new(_created.Log);

    /// <summary>
    /// Gets the builder for appending the events under specification and returning the first failure or final result.
    /// </summary>
    public EventScenarioWhenBuilder When => new(_created.Log);

    /// <summary>
    /// Gets the client event log backed by the selected in-process sequence.
    /// </summary>
    public IEventLog EventLog => _created.Log;

    /// <summary>
    /// Gets the selected sequence, the same instance as <see cref="EventLog"/>.
    /// </summary>
    public IEventSequence EventSequence => _created.Log;

    /// <summary>
    /// Gets the shared store for internal harness specifications.
    /// </summary>
    internal TestingEventStore TestingStore => _created.Root;

    /// <inheritdoc/>
    public void Dispose() => _created.Root.Connection.Dispose();

    /// <summary>
    /// Reads protected content for harness regression specifications.
    /// </summary>
    /// <param name="sequenceNumber">The stored position.</param>
    /// <returns>The stored JSON.</returns>
    internal async Task<string> ReadContentAtRest(EventSequenceNumber sequenceNumber)
    {
        var sequence = _created.Root.Store.GetNamespace(new KernelConcepts::Cratis.Chronicle.Concepts.EventStoreNamespaceName(namespaceName.Value))
            .GetEventSequence(new(eventSequenceId.Value));
        var stored = await sequence.GetEventAt(new(sequenceNumber.Value));
        return JsonSerializer.Serialize(stored.Content);
    }

    static (IEventLog Log, TestingEventStore Root) Create(EventSequenceId sequence, EventStoreName store, EventStoreNamespaceName ns, ICanProvideConstraints? provider, Defaults defaults)
    {
        var root = new TestingEventStore(store, ns, () => defaults.EventTypes);
        var eventSources = new EventSources.EventSources(null, defaults.ClientArtifactsProvider);
        eventSources.Discover().GetAwaiter().GetResult();
        root.Seed(provider, defaults.JsonSchemaGenerator, ((EventStoreForTesting)defaults.EventStore).EventTypeMigrators, eventSources.All).GetAwaiter().GetResult();
        var constraints = new InProcessConstraints(provider ?? new EmptyConstraintProvider());
        constraints.Discover().GetAwaiter().GetResult();
        var clientSequence = root.CreateSequence(sequence, defaults.EventSerializer, constraints, eventSources);
        return (clientSequence as IEventLog ?? EventLogForSequence.Create(clientSequence), root);
    }

    static ICanProvideConstraints DiscoverConstraints(Defaults defaults)
    {
        using var loggers = new NullLoggerFactory();
        return TestingEventStore.DiscoverConstraints(defaults.ClientArtifactsProvider, defaults.EventTypes, new ClientArtifactsActivator(((EventStoreForTesting)defaults.EventStore).ServiceProvider, loggers));
    }

    sealed class EmptyConstraintProvider : ICanProvideConstraints
    {
        public System.Collections.Immutable.IImmutableList<IConstraintDefinition> Provide() => [];
    }
}
