// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelSequenceConcepts = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a lightweight, in-process scenario for testing <see cref="IEventSequence"/> operations without any infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// The internal implementation uses the real kernel <c>EventSequence</c> grain instantiated directly — no Orleans silo
/// or Chronicle server required. Only the storage layer is in-memory. All business logic (constraint validation,
/// hash calculation, event serialization, migration, compliance) runs through the actual kernel code paths.
/// </para>
/// <para>
/// Use the <see cref="Given"/> property to seed pre-existing events into the event log before
/// exercising production code via <see cref="EventSequence"/> or <see cref="EventLog"/>.
/// </para>
/// <para>
/// Create a new <see cref="EventScenario"/> instance per test to keep tests isolated; the in-memory
/// event log accumulates state across calls on the same instance.
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
public class EventScenario
{
    readonly KernelBackedEventLog _eventLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class.
    /// </summary>
    public EventScenario()
        : this(
            EventSequenceId.Log,
            (KernelConceptsNs::EventStoreName)"test-event-store",
            (KernelConceptsNs::EventStoreNamespaceName)"default",
            constraintProvider: null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class with an explicit constraint provider.
    /// </summary>
    /// <param name="constraintProvider">The <see cref="ICanProvideConstraints"/> that supplies client-side constraint definitions. Pass <c>null</c> for no constraints.</param>
    public EventScenario(ICanProvideConstraints? constraintProvider)
        : this(
            EventSequenceId.Log,
            (KernelConceptsNs::EventStoreName)"test-event-store",
            (KernelConceptsNs::EventStoreNamespaceName)"default",
            constraintProvider)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventScenario"/> class with explicit event store coordinates and an optional constraint provider.
    /// </summary>
    /// <param name="eventSequenceId">The event sequence identifier.</param>
    /// <param name="eventStoreName">The event store name.</param>
    /// <param name="namespaceName">The event store namespace name.</param>
    /// <param name="constraintProvider">The <see cref="ICanProvideConstraints"/> that supplies client-side constraint definitions. Pass <c>null</c> for no constraints.</param>
    public EventScenario(
        EventSequenceId eventSequenceId,
        KernelConceptsNs::EventStoreName eventStoreName,
        KernelConceptsNs::EventStoreNamespaceName namespaceName,
        ICanProvideConstraints? constraintProvider)
    {
        _eventLog = CreateEventLog(eventSequenceId, eventStoreName, namespaceName, constraintProvider);
    }

    /// <summary>
    /// Gets the fluent builder used to seed pre-existing events into the event log before the act phase.
    /// </summary>
    public EventScenarioGivenBuilder Given => new(_eventLog);

    /// <summary>
    /// Gets the <see cref="IEventLog"/> backed by the real kernel event sequence grain for performing
    /// <c>Append</c> and <c>AppendMany</c> operations.
    /// </summary>
    public IEventLog EventLog => _eventLog;

    /// <summary>
    /// Gets the <see cref="IEventSequence"/> backed by the real kernel event sequence grain.
    /// </summary>
    /// <remarks>
    /// This is the same underlying instance as <see cref="EventLog"/>.
    /// </remarks>
    public IEventSequence EventSequence => _eventLog;

    static KernelBackedEventLog CreateEventLog(
        EventSequenceId eventSequenceId,
        KernelConceptsNs::EventStoreName eventStoreName,
        KernelConceptsNs::EventStoreNamespaceName namespaceName,
        ICanProvideConstraints? constraintProvider)
    {
        var kernelEventSequenceId = (KernelSequenceConcepts::EventSequenceId)(string)eventSequenceId;

        var eventSequenceStorage = new InMemoryEventSequenceStorage();
        var uniqueConstraintsStorage = new InMemoryUniqueConstraintsStorage();
        var uniqueEventTypesStorage = new InMemoryUniqueEventTypesConstraintsStorage();
        var constraintsStorage = new InMemoryConstraintsStorage(constraintProvider ?? new EmptyConstraintProvider());
        var identityStorage = new InMemoryIdentityStorage();
        var eventTypesStorage = new InMemoryEventTypesStorage();

        var storage = new InMemoryKernelStorage(
            eventSequenceStorage,
            uniqueConstraintsStorage,
            uniqueEventTypesStorage,
            constraintsStorage,
            identityStorage,
            eventTypesStorage,
            kernelEventSequenceId);

        var grain = InProcessEventSequence.Create(
            storage,
            kernelEventSequenceId,
            eventStoreName,
            namespaceName).GetAwaiter().GetResult();

        return new KernelBackedEventLog(grain);
    }

    sealed class EmptyConstraintProvider : ICanProvideConstraints
    {
        public IImmutableList<IConstraintDefinition> Provide() => ImmutableList<IConstraintDefinition>.Empty;
    }
}
