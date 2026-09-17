// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Traces;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.</param>
/// <param name="onCompleted">The action to call when the <see cref="IUnitOfWork"/> is completed.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
public class UnitOfWork(
    CorrelationId correlationId,
    Action<IUnitOfWork> onCompleted,
    IEventStore eventStore,
    IActivitySource<UnitOfWork>? activitySource = null) : IUnitOfWork
{
    /// <summary>
    /// Gets the default <see cref="IActivitySource{T}"/> for Chronicle client unit of work traces.
    /// </summary>
    internal static readonly IActivitySource<UnitOfWork> DefaultActivitySource =
        new ActivitySource<UnitOfWork>(new System.Diagnostics.ActivitySource(ClientActivity.SourceName));

    readonly IActivitySource<UnitOfWork> _activitySource = activitySource ?? DefaultActivitySource;
    readonly List<StagedEvents> _stagedEvents = [];
    readonly HashSet<EventSequenceId> _legacyEventSequenceIds = [];
    Dictionary<EventSourceId, ConcurrencyScope> _concurrencyScopes = [];

    AppendManyResult _appendManyResult = new();
    EventSequenceNumber? _lastCommittedEventSequenceNumber = EventSequenceNumber.Unavailable;
    Action<IUnitOfWork> _onCompleted = onCompleted;
    bool _isCommitted;
    bool _isRolledBack;
    bool _hasOrderedBatch;
    IEventSequence? _eventSequence;
    EventSequenceId? _eventSequenceId;
    LegacyStagedEvents? _currentLegacyEvents;

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
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
    {
        var scope = concurrencyScope ?? ConcurrencyScope.NotSet;
        if (_hasOrderedBatch)
        {
            ThrowIfLabelIsNotSpecified(eventSourceId);
            scope = MaterializeConcurrencyScope(scope);
            EnsureEventSequenceCanBeUsed(eventSequenceId);
            ValidateConcurrencyScope(eventSourceId, scope, _concurrencyScopes);
            BindToEventSequence(eventSequenceId);
        }
        else
        {
            BindLegacyEventSequence(eventSequenceId);
        }

        if (_currentLegacyEvents is null)
        {
            _currentLegacyEvents = new LegacyStagedEvents();
            _stagedEvents.Add(_currentLegacyEvents);
        }

        _currentLegacyEvents.Add(new EventForEventSourceId(eventSourceId, @event, causation)
        {
            EventStreamType = eventStreamType ?? EventStreamType.All,
            EventStreamId = eventStreamId ?? EventStreamId.Default,
            EventSourceType = eventSourceType ?? EventSourceType.Default,
            Tags = tags ?? [],
            Occurred = occurred,
            Subject = subject
        });
        if (_hasOrderedBatch)
        {
            EnrollStrictConcurrencyScope(eventSourceId, scope);
        }
        else
        {
            SetLegacyConcurrencyScope(eventSourceId, scope);
        }
    }

    /// <inheritdoc/>
    public void AddEvents(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        var batch = new EventsWithConcurrencyScopes(events, concurrencyScopes);
        ValidateLegacyEventSequenceIdsForOrderedBatch(eventSequenceId);
        EnsureEventSequenceCanBeUsed(eventSequenceId);
        ValidateExistingEventTargetsForOrderedBatch();
        var materializedConcurrencyScopes = MaterializeConcurrencyScopes(_concurrencyScopes);
        ValidateExistingConcurrencyScopesForOrderedBatch(materializedConcurrencyScopes);
        ValidateConcurrencyScopes(batch.ConcurrencyScopes, materializedConcurrencyScopes);

        BindToEventSequence(eventSequenceId);
        _concurrencyScopes = materializedConcurrencyScopes;
        _hasOrderedBatch = true;
        _currentLegacyEvents = null;
        _stagedEvents.Add(new OrderedStagedEvents(batch.Events));
        foreach (var (scopeLabel, concurrencyScope) in batch.ConcurrencyScopes)
        {
            EnrollStrictConcurrencyScope(scopeLabel, concurrencyScope);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<ConstraintViolation> GetConstraintViolations() => [.. _appendManyResult.ConstraintViolations];

    /// <inheritdoc/>
    public IEnumerable<ConcurrencyViolation> GetConcurrencyViolations() => _appendManyResult.ConcurrencyViolations;

    /// <inheritdoc/>
    public IEnumerable<object> GetEvents() => GetEventsToCommit().Select(_ => _.Event).ToArray();

    /// <inheritdoc/>
    public IEnumerable<AppendError> GetAppendErrors() => [.. _appendManyResult.Errors];

    /// <inheritdoc/>
    public async Task Commit()
    {
        using var span = _activitySource.Commit(correlationId.ToString());

        ThrowIfUnitOfWorkIsCompleted();

        try
        {
            if (_eventSequence is not null)
            {
                var result = await _eventSequence.AppendMany(GetEventsToCommit(), concurrencyScopes: _concurrencyScopes);
                if (result.SequenceNumbers?.Any() == true)
                {
                    _lastCommittedEventSequenceNumber = result.SequenceNumbers.MaxBy(_ => _.Value);
                }
                _appendManyResult = result;
            }
        }
        finally
        {
            // Completion must run even when the append throws (RpcException, unknown event type,
            // serialization error) - otherwise the unit leaks in the manager's dictionary and the
            // AsyncLocal Current keeps pointing at a completed unit. The exception still propagates.
            _isCommitted = true;
            _onCompleted(this);
        }
    }

    /// <inheritdoc/>
    public Task Rollback()
    {
        using var span = _activitySource.Rollback(correlationId.ToString());

        ThrowIfUnitOfWorkIsCompleted();
        _isRolledBack = true;
        _stagedEvents.Clear();
        _currentLegacyEvents = null;
        _concurrencyScopes.Clear();
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
        Rollback().GetAwaiter().GetResult();
    }

    static bool ConcurrencyScopesAreSemanticallyEqual(ConcurrencyScope first, ConcurrencyScope second)
    {
        if (first.SequenceNumber != second.SequenceNumber ||
            first.EventSourceId != second.EventSourceId ||
            first.EventStreamType != second.EventStreamType ||
            first.EventStreamId != second.EventStreamId ||
            first.EventSourceType != second.EventSourceType)
        {
            return false;
        }

        var firstEventTypes = first.EventTypes?.ToHashSet() ?? [];
        var secondEventTypes = second.EventTypes?.ToHashSet() ?? [];
        return firstEventTypes.SetEquals(secondEventTypes);
    }

    static ConcurrencyScope MaterializeConcurrencyScope(ConcurrencyScope concurrencyScope) =>
        concurrencyScope.EventTypes is null
            ? concurrencyScope
            : concurrencyScope with { EventTypes = concurrencyScope.EventTypes.ToArray() };

    static Dictionary<EventSourceId, ConcurrencyScope> MaterializeConcurrencyScopes(
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> concurrencyScopes) =>
        concurrencyScopes.ToDictionary(_ => _.Key, _ => MaterializeConcurrencyScope(_.Value));

    static void ThrowIfLabelIsNotSpecified(EventSourceId label)
    {
        if (label == EventSourceId.Unspecified || string.IsNullOrWhiteSpace(label.Value))
        {
            throw new ConcurrencyScopeLabelMustBeSpecified();
        }
    }

    void ThrowIfUnitOfWorkIsCompleted()
    {
        if (_isCommitted) throw new UnitOfWorkIsAlreadyCommitted(CorrelationId);
        if (_isRolledBack) throw new UnitOfWorkIsAlreadyRolledBack(CorrelationId);
    }

    void SetLegacyConcurrencyScope(EventSourceId scopeLabel, ConcurrencyScope concurrencyScope)
    {
        if (concurrencyScope == ConcurrencyScope.NotSet && _concurrencyScopes.ContainsKey(scopeLabel))
        {
            return;
        }

        if (concurrencyScope == ConcurrencyScope.NotSet)
        {
            _concurrencyScopes.Remove(scopeLabel);
            return;
        }

        _concurrencyScopes[scopeLabel] = concurrencyScope;
    }

    void EnrollStrictConcurrencyScope(EventSourceId scopeLabel, ConcurrencyScope concurrencyScope)
    {
        if (concurrencyScope == ConcurrencyScope.NotSet)
        {
            return;
        }

        _concurrencyScopes.TryAdd(scopeLabel, concurrencyScope);
    }

    void EnsureEventSequenceCanBeUsed(EventSequenceId eventSequenceId)
    {
        if (_eventSequenceId is not null && _eventSequenceId != eventSequenceId)
        {
            throw new UnitOfWorkCannotSpanEventSequences(_eventSequenceId, eventSequenceId);
        }
    }

    void BindToEventSequence(EventSequenceId eventSequenceId)
    {
        if (_eventSequence is not null)
        {
            return;
        }

        _eventSequence = eventStore.GetEventSequence(eventSequenceId);
        _eventSequenceId = eventSequenceId;
    }

    void BindLegacyEventSequence(EventSequenceId eventSequenceId)
    {
        var eventSequence = eventStore.GetEventSequence(eventSequenceId);
        _legacyEventSequenceIds.Add(eventSequenceId);
        if (_eventSequence is not null)
        {
            return;
        }

        _eventSequence = eventSequence;
        _eventSequenceId = eventSequenceId;
    }

    void ValidateConcurrencyScopes(
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> enrolledConcurrencyScopes)
    {
        foreach (var (scopeLabel, concurrencyScope) in concurrencyScopes)
        {
            ValidateConcurrencyScope(scopeLabel, concurrencyScope, enrolledConcurrencyScopes);
        }
    }

    void ValidateExistingConcurrencyScopesForOrderedBatch(IReadOnlyDictionary<EventSourceId, ConcurrencyScope> concurrencyScopes)
    {
        foreach (var (scopeLabel, concurrencyScope) in concurrencyScopes)
        {
            if (concurrencyScope.EventSourceId is not null && concurrencyScope.EventSourceId != scopeLabel)
            {
                throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope.EventSourceId);
            }
        }
    }

    void ValidateConcurrencyScope(
        EventSourceId scopeLabel,
        ConcurrencyScope concurrencyScope,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope> enrolledConcurrencyScopes)
    {
        if (concurrencyScope.EventSourceId is not null && concurrencyScope.EventSourceId != scopeLabel)
        {
            throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope.EventSourceId);
        }

        if (concurrencyScope == ConcurrencyScope.NotSet || !enrolledConcurrencyScopes.TryGetValue(scopeLabel, out var enrolledScope))
        {
            return;
        }

        if (!ConcurrencyScopesAreSemanticallyEqual(enrolledScope, concurrencyScope))
        {
            throw new ConflictingConcurrencyScopesForLabel(scopeLabel, enrolledScope, concurrencyScope);
        }
    }

    void ValidateExistingEventTargetsForOrderedBatch()
    {
        foreach (var @event in _stagedEvents.SelectMany(_ => _.GetEvents()))
        {
            ThrowIfLabelIsNotSpecified(@event.EventSourceId);
        }
    }

    void ValidateLegacyEventSequenceIdsForOrderedBatch(EventSequenceId eventSequenceId)
    {
        var mismatchedEventSequenceId = _legacyEventSequenceIds.FirstOrDefault(_ => _ != eventSequenceId);
        if (mismatchedEventSequenceId is not null)
        {
            throw new UnitOfWorkCannotSpanEventSequences(mismatchedEventSequenceId, eventSequenceId);
        }
    }

    EventForEventSourceId[] GetEventsToCommit() =>
        _stagedEvents.SelectMany(_ => _.GetEvents()).ToArray();

    abstract class StagedEvents
    {
        public abstract IEnumerable<EventForEventSourceId> GetEvents();
    }

    sealed class LegacyStagedEvents : StagedEvents
    {
        readonly Dictionary<EventSourceId, List<EventForEventSourceId>> _eventsBySource = [];

        public void Add(EventForEventSourceId @event)
        {
            if (!_eventsBySource.TryGetValue(@event.EventSourceId, out var events))
            {
                events = [];
                _eventsBySource[@event.EventSourceId] = events;
            }

            events.Add(@event);
        }

        public override IEnumerable<EventForEventSourceId> GetEvents() => _eventsBySource.Values.SelectMany(_ => _);
    }

    sealed class OrderedStagedEvents(IReadOnlyList<EventForEventSourceId> events) : StagedEvents
    {
        public override IEnumerable<EventForEventSourceId> GetEvents() => events;
    }
}
