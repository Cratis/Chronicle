// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutation"/>.
/// </summary>
/// <param name="eventSourceId">The <see cref="EventSourceId"/> for the aggregate root.</param>
/// <param name="mutator">The <see cref="IAggregateRootMutator"/> for the aggregate root.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the aggregate root.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> for the aggregate root.</param>
public class AggregateRootMutation(
    EventSourceId eventSourceId,
    IAggregateRootMutator mutator,
    IEventSequence eventSequence,
    ICausationManager causationManager) : IAggregateRootMutation
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

    readonly IList<object> _uncommittedEvents = [];

    /// <inheritdoc/>
    public EventSourceId EventSourceId => eventSourceId;

    /// <inheritdoc/>
    public IImmutableList<object> UncommittedEvents => _uncommittedEvents.ToImmutableList();

    /// <inheritdoc/>
    public async Task Apply<TEvent>(TEvent @event)
        where TEvent : class
    {
        typeof(TEvent).ValidateEventType();
        _uncommittedEvents.Add(@event);

        await mutator.Mutate(@event);
    }

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        causationManager.Add(CausationType, new Dictionary<string, string>
        {
            { CausationAggregateRootTypeProperty, GetType().AssemblyQualifiedName! },
            { CausationEventSequenceIdProperty, eventSequence.Id }
        });

        await eventSequence.AppendMany(eventSourceId, [.. _uncommittedEvents]);

        var result = new AggregateRootCommitResult(true, _uncommittedEvents.ToImmutableList());
        _uncommittedEvents.Clear();

        await mutator.Dehydrate();
        return result;
    }
}
