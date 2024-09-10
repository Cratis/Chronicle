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
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> for the aggregate root.</param>
/// <param name="mutator">The <see cref="IAggregateRootMutator"/> for the aggregate root.</param>
/// <param name="eventSequence">The <see cref="IEventSequence"/> for the aggregate root.</param>
public class AggregateRootMutation(
    IAggregateRootContext aggregateRootContext,
    IAggregateRootMutator mutator,
    IEventSequence eventSequence) : IAggregateRootMutation
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

    /// <inheritdoc/>
    public EventSourceId EventSourceId => aggregateRootContext.EventSourceId;

    /// <inheritdoc/>
    public IImmutableList<object> UncommittedEvents { get; private set; } = ImmutableList<object>.Empty;

    /// <inheritdoc/>
    public bool HasEvents => UncommittedEvents.Count > 0;

    /// <inheritdoc/>
    public IAggregateRootMutator Mutator => mutator;

    /// <inheritdoc/>
    public async Task Apply(object @event)
    {
        @event.GetType().ValidateEventType();
        var causation = new Causation(DateTimeOffset.Now, CausationType, new Dictionary<string, string>
        {
            { CausationAggregateRootTypeProperty, aggregateRootContext.AggregateRoot.GetType().AssemblyQualifiedName! },
            { CausationEventSequenceIdProperty, eventSequence.Id }
        });
        aggregateRootContext.UnitOfWOrk.AddEvent(eventSequence.Id, EventSourceId, @event, causation);
        UncommittedEvents = UncommittedEvents.Add(@event);

        await mutator.Mutate(@event);
    }

    /// <inheritdoc/>
    public async Task<AggregateRootCommitResult> Commit()
    {
        await aggregateRootContext.UnitOfWOrk.Commit();
        UncommittedEvents = ImmutableList<object>.Empty;
        return AggregateRootCommitResult.CreateFrom(aggregateRootContext.UnitOfWOrk);
    }
}
