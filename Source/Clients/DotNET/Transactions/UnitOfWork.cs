// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Collections;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.</param>
/// <param name="onCompleted">The action to call when the <see cref="IUnitOfWork"/> is completed.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
public class UnitOfWork(
    CorrelationId correlationId,
    Action<IUnitOfWork> onCompleted,
    IEventStore eventStore) : IUnitOfWork
{
    readonly ConcurrentDictionary<EventSequenceId, ConcurrentBag<EventForEventSourceId>> _events = [];
    readonly ConcurrentBag<ConstraintViolation> _constraintViolations = [];
    readonly ConcurrentBag<AppendError> _appendErrors = [];
    Action<IUnitOfWork> _onCompleted = onCompleted;
    bool _isCommitted;
    bool _isRolledBack;

    /// <inheritdoc/>
    public CorrelationId CorrelationId => correlationId;

    /// <inheritdoc/>
    public bool IsSuccess => _constraintViolations.IsEmpty && _appendErrors.IsEmpty;

    /// <inheritdoc/>
    public void AddEvent(EventSequenceId eventSequenceId, EventSourceId eventSourceId, object @event, Causation causation)
    {
        if (!_events.TryGetValue(eventSequenceId, out var events))
        {
            _events[eventSequenceId] = events = [];
        }

        events.Add(new(eventSourceId, @event, causation));
    }

    /// <inheritdoc/>
    public IImmutableList<ConstraintViolation> GetConstraintViolations() =>
        _constraintViolations.ToImmutableList();

    /// <inheritdoc/>
    public IImmutableList<object> GetEvents() =>
        _events.Values.SelectMany(_ => _).Select(_ => _.Event).ToImmutableList();

    /// <inheritdoc/>
    public IImmutableList<AppendError> GetAppendErrors() =>
        _appendErrors.ToImmutableList();

    /// <inheritdoc/>
    public async Task Commit()
    {
        ThrowIfUnitOfWorkIsCompleted();
        _isCommitted = true;

        foreach (var (eventSequenceId, events) in _events)
        {
            var eventSequence = eventStore.GetEventSequence(eventSequenceId);
            var result = await eventSequence.AppendMany(events);
            result.ConstraintViolations.ForEach(_constraintViolations.Add);
            result.Errors.ForEach(_appendErrors.Add);
        }

        _onCompleted(this);
    }

    /// <inheritdoc/>
    public Task Rollback()
    {
        ThrowIfUnitOfWorkIsCompleted();
        _isRolledBack = true;
        _events.Clear();
        _constraintViolations.Clear();

        _onCompleted(this);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void OnCompleted(Action<IUnitOfWork> callback) => _onCompleted = callback;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_isCommitted && !_isRolledBack)
        {
            Rollback().Wait();
        }

        _onCompleted(this);
    }

    void ThrowIfUnitOfWorkIsCompleted()
    {
        if (_isCommitted) throw new UnitOfWorkIsAlreadyCommitted(CorrelationId);
        if (_isRolledBack) throw new UnitOfWorkIsAlreadyRolledBack(CorrelationId);
    }
}
