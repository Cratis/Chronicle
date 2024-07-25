// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Aggregates;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// Represents an implementation of <see cref="IAggregateRoot"/>.
/// </summary>
public class AggregateRoot : IAggregateRoot
{
    /// <summary>
    /// The causation aggregate root type property.
    /// </summary>
    public const string CausationAggregateRootTypeProperty = "aggregateRootType";

    /// <summary>
    /// The event sequence id causation property.
    /// </summary>
    public const string CausationEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The causation type for the aggregate root.
    /// </summary>
    public static readonly CausationType CausationType = "AggregateRoot";

    /// <summary>
    /// Chronicle Internal: The uncommitted events for the aggregate root.
    /// </summary>
    internal readonly List<object> _uncommittedEvents = [];

    /// <summary>
    /// Chronicle Internal: The event handlers for the aggregate root.
    /// </summary>
    internal IAggregateRootEventHandlers EventHandlers = default!;

    /// <summary>
    /// Chronicle Internal: The state provider for the aggregate root.
    /// </summary>
    internal IAggregateRootStateProvider StateProvider = default!;

    /// <summary>
    /// Chronicle Internal: The event sequence for the aggregate root.
    /// </summary>
    internal IEventSequence EventSequence = default!;

    /// <summary>
    /// Chronicle Internal: The <see cref="ICausationManager"/> to use with the aggregate root when committing.
    /// </summary>
    internal ICausationManager CausationManager = default!;

    /// <summary>
    /// Chronicle Internal: The <see cref="_eventSourceId"/> for the aggregate root.
    /// </summary>
    internal EventSourceId _eventSourceId = EventSourceId.Unspecified;

    /// <summary>
    /// Chronicle Internal: The <see cref="CorrelationId"/> for the aggregate root.
    /// </summary>
    internal CorrelationId CorrelationId = CorrelationId.New();

    /// <inheritdoc/>
    public virtual bool IsStateful => false;

    /// <inheritdoc/>
    public virtual EventSequenceId EventSequenceId => EventSequenceId.Log;

    /// <summary>
    /// Chronicle Internal: The type of state for the aggregate root.
    /// </summary>
    internal virtual Type StateType => typeof(object);

    /// <summary>
    /// Gets the <see cref="EventSourceId"/> for the aggregate root.
    /// </summary>
    protected EventSourceId EventSourceId => _eventSourceId;

    /// <inheritdoc/>
    public async Task Apply<T>(T @event)
        where T : class
    {
        typeof(T).ValidateEventType();
        _uncommittedEvents.Add(@event);

        MutateState(await StateProvider.Update(GetState(), [@event]));

        if (!IsStateful)
        {
            // TODO: We should have the correct event store and namespace, this should be injected into the aggregate root.
            await EventHandlers.Handle(
                this,
                [
                    new EventAndContext(
                        @event,
                        EventContext.From(EventStoreName.NotSet, EventStoreNamespaceName.NotSet, EventSourceId, EventSequenceNumber.Unavailable))
                ]);
        }
    }

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        CausationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationAggregateRootTypeProperty, GetType().AssemblyQualifiedName! },
            { CausationEventSequenceIdProperty, EventSequenceId.ToString() }
        });

        await EventSequence.AppendMany(_eventSourceId, [.. _uncommittedEvents]);

        var result = new AggregateRootCommitResult(true, _uncommittedEvents.ToImmutableList());
        _uncommittedEvents.Clear();

        await StateProvider.Dehydrate();
        return result;
    }

    /// <summary>
    /// Chronicle Internal: Set the state for the aggregate root.
    /// </summary>
    /// <param name="state">State to set.</param>
    internal virtual void MutateState(object? state)
    {
    }

    /// <summary>
    /// Chronicle Internal: Get the state for the aggregate root.
    /// </summary>
    /// <returns>The current state.</returns>
    internal virtual object? GetState() => null;

    /// <summary>
    /// Chronicle Internal: Invoke the OnActivate method.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task InternalOnActivate() => OnActivate();

    /// <summary>
    /// Called when the aggregate root is ready to be activated.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    protected virtual Task OnActivate() => Task.CompletedTask;
}

/// <summary>
/// Represents a stateful implementation of <see cref="IAggregateRoot"/>.
/// </summary>
/// <typeparam name="TState">Type of state for the aggregate root.</typeparam>
public class AggregateRoot<TState> : AggregateRoot
    where TState : class
{
    /// <summary>
    /// State of the aggregate root - accessible only to Chronicle Internally.
    /// </summary>
    internal TState _state = default!;

    /// <inheritdoc/>
    public override bool IsStateful => true;

    /// <inheritdoc/>
    internal override Type StateType => typeof(TState);

    /// <summary>
    /// Gets the current state of the aggregate root.
    /// </summary>
    protected TState? State => _state;

    /// <inheritdoc/>
    internal override void MutateState(object? state) => _state = (state as TState)!;

    /// <inheritdoc/>
    internal override object? GetState() => _state;
}
