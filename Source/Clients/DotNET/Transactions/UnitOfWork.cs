// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
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
    readonly ConcurrentDictionary<EventSequenceId, ConcurrentBag<EventForEventSourceIdWithSequenceNumber>> _events = [];
    readonly ConcurrentBag<ConstraintViolation> _constraintViolations = [];
    readonly ConcurrentBag<AppendError> _appendErrors = [];

    EventSequenceNumber? _lastCommittedEventSequenceNumber;
    Action<IUnitOfWork> _onCompleted = onCompleted;
    bool _isCommitted;
    bool _isRolledBack;
    EventSequenceNumber _currentSequenceNumber = EventSequenceNumber.First;

    /// <inheritdoc/>
    public bool IsCompleted => _isCommitted || _isRolledBack;

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

        events.Add(new(_currentSequenceNumber, eventSourceId, @event, causation));

        _currentSequenceNumber++;
    }

    /// <inheritdoc/>
    public IEnumerable<ConstraintViolation> GetConstraintViolations() => [.. _constraintViolations];

    /// <inheritdoc/>
    public IEnumerable<object> GetEvents() =>
        _events.Values.SelectMany(_ => _)
            .OrderBy(_ => _.SequenceNumber.Value)
            .Select(_ => _.Event).ToArray();

    /// <inheritdoc/>
    public IEnumerable<AppendError> GetAppendErrors() => [.. _appendErrors];

    /// <inheritdoc/>
    public async Task Commit()
    {
        ThrowIfUnitOfWorkIsCompleted();

        foreach (var (eventSequenceId, events) in _events)
        {
            var sorted = events
                            .OrderBy(_ => _.SequenceNumber)
                            .Select(e => new EventForEventSourceId(e.EventSourceId, e.Event, e.Causation))
                            .ToArray();
            var eventSequence = eventStore.GetEventSequence(eventSequenceId);
            var result = await eventSequence.AppendMany(sorted, correlationId);
            result.ConstraintViolations.ForEach(_constraintViolations.Add);
            result.Errors.ForEach(_appendErrors.Add);
            _lastCommittedEventSequenceNumber = result.SequenceNumbers.OrderBy(_ => _.Value).LastOrDefault();
        }

        _isCommitted = true;
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
    public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
    {
        eventSequenceNumber = _lastCommittedEventSequenceNumber;
        return eventSequenceNumber is not null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsCompleted)
        {
            return;
        }
        if (!_isCommitted && !_isRolledBack)
        {
            Rollback().GetAwaiter().GetResult();
        }

        _onCompleted(this);
    }

    void ThrowIfUnitOfWorkIsCompleted()
    {
        if (_isCommitted) throw new UnitOfWorkIsAlreadyCommitted(CorrelationId);
        if (_isRolledBack) throw new UnitOfWorkIsAlreadyRolledBack(CorrelationId);
    }
}
