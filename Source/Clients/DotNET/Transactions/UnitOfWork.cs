// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences.Operations;

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
    AppendManyResult _appendManyResult = new();
    EventSequenceNumber? _lastCommittedEventSequenceNumber = EventSequenceNumber.Unavailable;
    Action<IUnitOfWork> _onCompleted = onCompleted;
    bool _isCommitted;
    bool _isRolledBack;
    EventSequenceOperations? _eventSequenceOperations;

    /// <inheritdoc/>
    public bool IsCompleted => _isCommitted || _isRolledBack;

    /// <inheritdoc/>
    public CorrelationId CorrelationId => correlationId;

    /// <inheritdoc/>
    public bool IsSuccess => _appendManyResult.IsSuccess;

    /// <inheritdoc/>
    public void AddEvent(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        object @event,
        Causation causation,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default)
    {
        var eventSequence = eventStore.GetEventSequence(eventSequenceId);
        _eventSequenceOperations ??= new EventSequenceOperations(eventSequence);
        _eventSequenceOperations = _eventSequenceOperations
            .ForEventSourceId(eventSourceId, builder => builder
            .WithConcurrencyScope(concurrencyScope ?? ConcurrencyScope.NotSet)
            .Append(
                @event,
                causation,
                eventStreamType,
                eventStreamId,
                eventSourceType));
    }

    /// <inheritdoc/>
    public IEnumerable<ConstraintViolation> GetConstraintViolations() => [.. _appendManyResult.ConstraintViolations];

    /// <inheritdoc/>
    public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => _appendManyResult.ConcurrencyViolations;

    /// <inheritdoc/>
    public IEnumerable<object> GetEvents() =>
        _eventSequenceOperations?.GetAppendedEvents() ?? [];

    /// <inheritdoc/>
    public IEnumerable<AppendError> GetAppendErrors() => [.. _appendManyResult.Errors];

    /// <inheritdoc/>
    public async Task Commit()
    {
        ThrowIfUnitOfWorkIsCompleted();

        if (_eventSequenceOperations is not null)
        {
            var result = await _eventSequenceOperations.Perform();
            if (result.SequenceNumbers?.Any() == true)
            {
                _lastCommittedEventSequenceNumber = result.SequenceNumbers.MaxBy(_ => _.Value);
            }
            _appendManyResult = result;
        }

        _isCommitted = true;
        _onCompleted(this);
    }

    /// <inheritdoc/>
    public Task Rollback()
    {
        ThrowIfUnitOfWorkIsCompleted();
        _isRolledBack = true;
        _eventSequenceOperations?.Clear();
        _appendManyResult = AppendManyResult.Success(CorrelationId.NotSet, []);

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
