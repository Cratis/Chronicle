// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="ITransactionalEventSequence"/>.
/// </summary>
/// <param name="eventSequence">The target <see cref="IEventSequence"/>.</param>
/// <param name="unitOfWorkManager"><see cref="IUnitOfWorkManager"/> for working with the unit of work.</param>
public class TransactionalEventSequence(IEventSequence eventSequence, IUnitOfWorkManager unitOfWorkManager) : ITransactionalEventSequence
{
    /// <summary>
    /// The event sequence id causation property.
    /// </summary>
    public const string TransactionalEventSequenceIdProperty = "eventSequenceId";

    /// <summary>
    /// The causation type for the transactional event sequence.
    /// </summary>
    public static readonly CausationType CausationType = "TransactionalEventSequence";

    /// <inheritdoc/>
    public IUnitOfWork UnitOfWork => unitOfWorkManager.Current;

    /// <inheritdoc/>
    public Task Append(
        EventSourceId eventSourceId,
        object @event,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default)
    {
        var causation = GetCausation();
        UnitOfWork.AddEvent(eventSequence.Id, eventSourceId, @event, causation, eventStreamType, eventStreamId, eventSourceType);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task AppendMany(
        EventSourceId eventSourceId,
        IEnumerable<object> events,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null,
        EventSourceType? eventSourceType = null,
        ConcurrencyScope? concurrencyScope = default)
    {
        var causation = GetCausation();
        foreach (var @event in events)
        {
            UnitOfWork.AddEvent(eventSequence.Id, eventSourceId, @event, causation, eventStreamType, eventStreamId, eventSourceType);
        }
        return Task.CompletedTask;
    }

    Causation GetCausation()
    {
        return new Causation(DateTimeOffset.Now, CausationType, new Dictionary<string, string>
        {
            { TransactionalEventSequenceIdProperty, eventSequence.Id }
        });
    }
}
