// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWork"/>.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.</param>
/// <param name="onCompleted">The action to call when the <see cref="IUnitOfWork"/> is completed.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
/// <param name="lifecyclePolicy">The policy for staging events once completion begins.</param>
/// <param name="logger">Optional logger for late staging diagnostics.</param>
public class UnitOfWork(
    CorrelationId correlationId,
    Action<IUnitOfWork> onCompleted,
    IEventStore eventStore,
    IActivitySource<UnitOfWork>? activitySource,
    UnitOfWorkLifecyclePolicy lifecyclePolicy,
    ILogger<UnitOfWork>? logger = null) : IUnitOfWork
{
    /// <summary>
    /// Gets the default <see cref="IActivitySource{T}"/> for Chronicle client unit of work traces.
    /// </summary>
    internal static readonly IActivitySource<UnitOfWork> DefaultActivitySource =
        new ActivitySource<UnitOfWork>(new System.Diagnostics.ActivitySource(ClientActivity.SourceName));

    readonly IActivitySource<UnitOfWork> _activitySource = activitySource ?? DefaultActivitySource;
    readonly ILogger<UnitOfWork>? _logger = logger;
    readonly List<StagedEvents> _stagedEvents = [];
    readonly HashSet<EventSequenceId> _legacyEventSequenceIds = [];
    readonly object _decisionLock = new();
    readonly Dictionary<EventSourceId, ConcurrencyScope> _decisionScopes = [];
    readonly Dictionary<EventSourceId, List<DecisionConflict>> _decisionConflicts = [];
    Dictionary<EventSourceId, ConcurrencyScope> _concurrencyScopes = [];

    AppendManyResult _appendManyResult = new();
    EventSequenceNumber? _lastCommittedEventSequenceNumber = EventSequenceNumber.Unavailable;
    Action<IUnitOfWork> _onCompleted = onCompleted;
    bool _isCommitted;
    bool _isRolledBack;
    bool _completing;
    bool _hasOrderedBatch;
    bool _hasEnrolledDecisionReads;
    IEventSequence? _eventSequence;
    EventSequenceId? _eventSequenceId;
    LegacyStagedEvents? _currentLegacyEvents;
    DecisionReadCommitOwner? _commitOwner;

    /// <summary>
    /// Initializes a unit of work with compatibility staging behavior.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="onCompleted">Called on completion.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="activitySource">Optional activity source for tracing.</param>
    public UnitOfWork(CorrelationId correlationId, Action<IUnitOfWork> onCompleted, IEventStore eventStore, IActivitySource<UnitOfWork>? activitySource = null)
        : this(correlationId, onCompleted, eventStore, activitySource, UnitOfWorkLifecyclePolicy.Compatibility)
    {
    }

    /// <summary>
    /// Initializes a unit of work with the given lifecycle policy.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="onCompleted">Called on completion.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="lifecyclePolicy">The lifecycle policy.</param>
    /// <param name="logger">Optional logger for late staging diagnostics.</param>
    public UnitOfWork(CorrelationId correlationId, Action<IUnitOfWork> onCompleted, IEventStore eventStore, UnitOfWorkLifecyclePolicy lifecyclePolicy, ILogger<UnitOfWork>? logger = null)
        : this(correlationId, onCompleted, eventStore, null, lifecyclePolicy, logger)
    {
    }

    /// <inheritdoc/>
    public bool IsCompleted => _isCommitted || _isRolledBack;

    /// <inheritdoc/>
    public CorrelationId CorrelationId => correlationId;

    /// <inheritdoc/>
    public bool IsSuccess => _appendManyResult.IsSuccess;

    /// <inheritdoc/>
    public bool HasEnrolledDecisionReads
    {
        get
        {
            lock (_decisionLock)
            {
                return _hasEnrolledDecisionReads;
            }
        }
    }

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
        Subject? subject = default) =>
        AddEventWithNamedTags(eventSequenceId, eventSourceId, @event, [], causation, eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject);

    /// <inheritdoc/>
    public void AddEventWithNamedTags(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        object @event,
        IEnumerable<NamedTag> namedTags,
        Causation causation,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        EventSourceType? eventSourceType = default,
        ConcurrencyScope? concurrencyScope = default,
        IEnumerable<string>? tags = default,
        DateTimeOffset? occurred = default,
        Subject? subject = default)
    {
        lock (_decisionLock)
        {
            ThrowIfStagingAfterCompletion();
            var scope = concurrencyScope ?? ConcurrencyScope.NotSet;
            if (_decisionScopes.Count != 0)
            {
                ValidateLegacyEventSequenceIdsForOrderedBatch(eventSequenceId);
                EnsureEventSequenceCanBeUsed(eventSequenceId);
            }
            if (scope != ConcurrencyScope.NotSet && _decisionScopes.TryGetValue(eventSourceId, out var decisionScope))
            {
                throw new ConflictingConcurrencyScopesForLabel(eventSourceId, decisionScope, scope);
            }
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
                NamedTags = NamedTagConverters.Merge([], namedTags),
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
    }

    /// <inheritdoc/>
    public void AddEvents(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes)
    {
        lock (_decisionLock)
        {
            ThrowIfStagingAfterCompletion();
            var batch = new EventsWithConcurrencyScopes(events, concurrencyScopes);
            foreach (var (label, scope) in batch.ConcurrencyScopes)
            {
                if (scope != ConcurrencyScope.NotSet && _decisionScopes.TryGetValue(label, out var decisionScope))
                {
                    throw new ConflictingConcurrencyScopesForLabel(label, decisionScope, scope);
                }
            }
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
            _stagedEvents.Add(new OrderedStagedEvents(batch.Events.Select(_ => _ with
            {
                NamedTags = NamedTagConverters.Merge(_.NamedTags, [])
            }).ToArray()));
            foreach (var (scopeLabel, concurrencyScope) in batch.ConcurrencyScopes)
            {
                EnrollStrictConcurrencyScope(scopeLabel, concurrencyScope);
            }
        }
    }

    /// <inheritdoc/>
    public void AddDecisionRead(IDecisionRead read)
    {
        lock (_decisionLock)
        {
            if (IsCompleted || _completing) throw new DecisionReadAfterCompletion();
            DecisionReadScopes.Validate(read, eventStore.Name, eventStore.Namespace, EventSequenceId.Log);
            ValidateLegacyEventSequenceIdsForOrderedBatch(EventSequenceId.Log);
            EnsureEventSequenceCanBeUsed(EventSequenceId.Log);
            var label = (EventSourceId)read.Key;
            if (_decisionScopes.TryGetValue(label, out var existing))
            {
                _decisionScopes[label] = DecisionReadScopes.Merge(existing, read.Scope);
            }
            else
            {
                if (_concurrencyScopes.TryGetValue(label, out var explicitScope))
                {
                    throw new ConflictingConcurrencyScopesForLabel(label, explicitScope, read.Scope);
                }
                _decisionScopes.Add(label, read.Scope);
            }
            if (!_decisionConflicts.TryGetValue(label, out var conflicts))
            {
                conflicts = [];
                _decisionConflicts.Add(label, conflicts);
            }
            var conflict = new DecisionConflict(read.ReadModelType, read.Key);
            if (!conflicts.Contains(conflict)) conflicts.Add(conflict);
            BindToEventSequence(EventSequenceId.Log);
            _hasEnrolledDecisionReads = true;
        }
    }

    /// <inheritdoc/>
    public IEnumerable<DecisionConflict> GetDecisionConflicts() =>
        _appendManyResult.ConcurrencyViolations
            .SelectMany(_ => _decisionConflicts.TryGetValue(_.EventSourceId, out var conflicts) ? conflicts : [])
            .Distinct().ToArray();

    /// <summary>Claims exclusive completion ownership for protected units of work.</summary>
    /// <returns>The opaque owner capability.</returns>
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">An owner has already claimed this unit.</exception>
    public DecisionReadCommitOwner ClaimDecisionReadCommitOwnership()
    {
        lock (_decisionLock)
        {
            ThrowIfUnitOfWorkIsCompleted();
            if (_commitOwner is not null) throw new ProtectedUnitOfWorkRequiresOwner();
            return _commitOwner = new DecisionReadCommitOwner();
        }
    }

    /// <summary>Commits using the capability obtained by the transaction owner.</summary>
    /// <param name="owner">The claimed owner capability.</param>
    /// <returns>The commit task.</returns>
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">A different owner attempted to complete the unit.</exception>
    public async Task CommitAsOwner(DecisionReadCommitOwner owner)
    {
        if (!ReferenceEquals(owner, _commitOwner)) throw new ProtectedUnitOfWorkRequiresOwner();
        await CommitCore(true);
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
    public Task Commit() => CommitCore(false);

    /// <inheritdoc/>
    public Task Rollback()
    {
        using var span = _activitySource.Rollback(correlationId.ToString());

        lock (_decisionLock)
        {
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing && lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict) throw new UnitOfWorkIsCompleting(correlationId);
            CompleteRollback();
        }

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
        lock (_decisionLock)
        {
            if (_completing || IsCompleted) return;

            using var span = _activitySource.Rollback(correlationId.ToString());
            CompleteRollback();
        }
        _onCompleted(this);
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

    async Task CommitCore(bool fromOwner)
    {
        using var span = _activitySource.Commit(correlationId.ToString());

        Dictionary<EventSourceId, ConcurrencyScope> scopes;
        EventForEventSourceId[] events;
        IEventSequence? eventSequence;
        bool protectedCommit;
        lock (_decisionLock)
        {
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new DecisionReadAfterCompletion();
            protectedCommit = _decisionScopes.Count != 0;
            if (protectedCommit && (!fromOwner || _commitOwner is null)) throw new ProtectedUnitOfWorkRequiresOwner();
            _completing = true;

            // Capture the commit batch under the same lock as enrollment. Compatibility calls can
            // still stage after this point, but they must never enter the in-flight append.
            eventSequence = _eventSequence;
            events = eventSequence is null ? [] : GetEventsToCommit();
            scopes = !protectedCommit ? _concurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value) :
                _concurrencyScopes.Concat(_decisionScopes).ToDictionary(_ => _.Key, _ => _.Value);
        }

        try
        {
            if (eventSequence is not null)
            {
                AppendManyResult result;
                try
                {
                    result = events.Any(_ => _.NamedTags.Any())
                        ? await eventSequence.AppendManyWithNamedTags(events, [], concurrencyScopes: scopes)
                        : await eventSequence.AppendMany(events, concurrencyScopes: scopes);
                }
                catch (CommandFailed exception) when (
                    protectedCommit && events.Length == 0 &&
                    exception.Result?.ValidationResults.Any(_ => _.Message == "At least one event is required.") == true)
                {
                    throw new DecisionReadValidateOnlyNotSupported();
                }
                if (result.SequenceNumbers?.Any() == true)
                {
                    _lastCommittedEventSequenceNumber = result.SequenceNumbers.MaxBy(_ => _.Value);
                }
                _appendManyResult = result;
            }
        }
        catch (Exception exception) when (protectedCommit)
        {
            _appendManyResult = AppendManyResult.Failed(correlationId, [new AppendError(exception.Message)]);
            throw;
        }
        finally
        {
            // Completion must run even when the append throws (RpcException, unknown event type,
            // serialization error) - otherwise the unit leaks in the manager's dictionary and the
            // AsyncLocal Current keeps pointing at a completed unit. The exception still propagates.
            lock (_decisionLock)
            {
                _isCommitted = true;
            }
            _onCompleted(this);
        }
    }

    void CompleteRollback()
    {
        _isRolledBack = true;
        _stagedEvents.Clear();
        _currentLegacyEvents = null;
        _concurrencyScopes.Clear();
        _appendManyResult = AppendManyResult.Success(CorrelationId.NotSet, []);
    }

    void ThrowIfStagingAfterCompletion()
    {
        if (!_completing && !IsCompleted) return;

        if (lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict)
        {
            throw new UnitOfWorkIsCompleted(correlationId);
        }

        if (_logger is not null)
        {
            _logger.EventsStagedAfterCompletion(correlationId);
        }
        else
        {
            Trace.TraceError("Events were staged after unit of work '{0}' began completing and will not be appended by this unit of work.", correlationId);
        }
        if (_decisionScopes.Count != 0)
        {
            throw new ProtectedUnitOfWorkEventsAfterCompletion();
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
