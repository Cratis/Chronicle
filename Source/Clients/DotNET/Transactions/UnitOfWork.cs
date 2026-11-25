// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.ReadModels;
using Cratis.Traces;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IUnitOfWork"/>.
/// </summary>
public partial class UnitOfWork : IUnitOfWork
{
    /// <summary>
    /// Gets the default <see cref="IActivitySource{T}"/> for Chronicle client unit of work traces.
    /// </summary>
    internal static readonly IActivitySource<UnitOfWork> DefaultActivitySource =
        new ActivitySource<UnitOfWork>(new ActivitySource(ClientActivity.SourceName));

    readonly IActivitySource<UnitOfWork> _activitySource;
    readonly Lazy<IEventStore> _eventStore;
    readonly UnitOfWorkLifecyclePolicy _lifecyclePolicy;
    readonly ILogger<UnitOfWork>? _logger;
    readonly List<StagedEvents> _stagedEvents = [];
    readonly HashSet<EventSequenceId> _legacyEventSequenceIds = [];
    readonly Lock _decisionLock = new();
    readonly Dictionary<EventSourceId, ConcurrencyScope> _decisionScopes = [];
    readonly Dictionary<EventSourceId, List<DecisionConflict>> _decisionConflicts = [];
    Dictionary<EventSourceId, List<ConcurrencyScope>> _concurrencyScopes = [];

    AppendManyResult _appendManyResult = new();
    EventSequenceNumber? _lastCommittedEventSequenceNumber = EventSequenceNumber.Unavailable;
    Action<IUnitOfWork> _onCompleted;
    bool _isCommitted;
    Exception? _commitFailure;
    bool _isRolledBack;
    bool _completing;
    bool _hasOrderedBatch;
    bool _hasEnrolledDecisionReads;
    IEventSequence? _eventSequence;
    EventSequenceId? _eventSequenceId;
    LegacyStagedEvents? _currentLegacyEvents;
    DecisionReadCommitOwner? _commitOwner;
    EffectKey? _effectKey;
    Contracts.Occurrences.EffectReceipt? _effectReceipt;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="onCompleted">Called on completion.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="activitySource">Optional activity source for tracing.</param>
    /// <param name="lifecyclePolicy">The policy for staging after completion begins.</param>
    /// <param name="logger">Optional logger for late staging diagnostics.</param>
    public UnitOfWork(
        CorrelationId correlationId,
        Action<IUnitOfWork> onCompleted,
        IEventStore eventStore,
        IActivitySource<UnitOfWork>? activitySource,
        UnitOfWorkLifecyclePolicy lifecyclePolicy,
        ILogger<UnitOfWork>? logger = null)
        : this(correlationId, onCompleted, () => eventStore, activitySource, lifecyclePolicy, logger)
    {
    }

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

    /// <summary>
    /// Initializes a unit whose event store is resolved only when an operation needs it.
    /// </summary>
    /// <param name="correlationId">The correlation identifier.</param>
    /// <param name="onCompleted">Called on completion.</param>
    /// <param name="eventStore">Resolves the event store in the owning scope.</param>
    /// <param name="activitySource">Optional activity source for tracing.</param>
    /// <param name="lifecyclePolicy">The policy for staging after completion begins.</param>
    /// <param name="logger">Optional logger for late staging diagnostics.</param>
    internal UnitOfWork(
        CorrelationId correlationId,
        Action<IUnitOfWork> onCompleted,
        Func<IEventStore> eventStore,
        IActivitySource<UnitOfWork>? activitySource,
        UnitOfWorkLifecyclePolicy lifecyclePolicy,
        ILogger<UnitOfWork>? logger)
    {
        CorrelationId = correlationId;
        _onCompleted = onCompleted;
        _eventStore = new(eventStore, LazyThreadSafetyMode.PublicationOnly);
        _activitySource = activitySource ?? DefaultActivitySource;
        _lifecyclePolicy = lifecyclePolicy;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsCompleted => _isCommitted || _isRolledBack;

    /// <inheritdoc/>
    public CorrelationId CorrelationId { get; }

    /// <inheritdoc/>
    public bool IsSuccess => _appendManyResult.IsSuccess;

    /// <inheritdoc/>
    public EffectReceiptResult? EffectReceipt { get; private set; }

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

    /// <summary>
    /// Gets or sets the <see cref="EventTagResolver"/> used to resolve the named tags of staged events at enrollment.
    /// </summary>
    internal EventTagResolver? TagResolver { get; set; }

    /// <summary>Gets the client span naming convention.</summary>
    internal ClientSpanNames SpanNames { get; init; }

    bool IsCompletionStrict => _immediateAppendRefusalOwnsCompletion || IsEnforcingReadProtection || _lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict;

    bool HasClaimedDecisionReads => _commitOwner is not null && _hasEnrolledDecisionReads;

    /// <inheritdoc/>
    public void UseEffectReceipt(EffectKey key, JsonNode? response = null)
    {
        ImmediateAppendGuard.RequireUnit(this);
        ReadProtectionGuard.RequireUnit(this);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new UnitOfWorkIsCompleting(CorrelationId);
            EffectReceiptSupport.Require(_eventStore.Value.Connection);
            if (_effectKey is not null && _effectKey != key) throw new UnitOfWorkEffectReceiptAlreadySet();
            var receipt = EffectReceiptSupport.ToContract(key, response);
            _effectKey = key;
            _effectReceipt = receipt;
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
        Subject? subject = default) =>
        AddEventWithNamedTagsCore(eventSequenceId, eventSourceId, @event, namedTags, causation, eventStreamType, eventStreamId, eventSourceType, concurrencyScope, tags, occurred, subject, ConcurrencyScopeOrigin.Explicit);

    /// <inheritdoc/>
    public void AddEvents(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes) =>
        AddEvents(eventSequenceId, events, concurrencyScopes, ConcurrencyScopeOrigin.Explicit);

    /// <inheritdoc/>
    public void AddEvents(
        EventSequenceId eventSequenceId,
        IEnumerable<EventForEventSourceId> events,
        IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>> concurrencyScopes,
        ConcurrencyScopeOrigin origin)
    {
        ConcurrencyScopeOriginNotValidForEvents.ThrowIfNotValid(origin);
        lock (_decisionLock)
        {
            ThrowIfStagingAfterCompletion();
            events = events.ToArray();
            if (events.Any(_ => _.IdempotencyKey is not null)) throw new IdempotentAppendsNotSupported("in a unit of work");
            if (IsEnforcingReadProtection)
            {
                AddEnforcedEvents(eventSequenceId, events, concurrencyScopes, origin);
                return;
            }
            var batch = CreateRequiredScopeBatch(eventSequenceId, events, concurrencyScopes);
            var enrollments = CreateEnrollments(eventSequenceId, batch);
            var resolution = ResolveEnrollmentScopes(eventSequenceId, enrollments);
            ThrowIfResolvedScopesConflictWithDecisionReads(resolution);
            var materializedConcurrencyScopes = ValidateOrderedBatch(eventSequenceId, resolution);
            var stagedEvents = ResolveBatchTags(eventSequenceId, batch);

            BindToEventSequence(eventSequenceId);
            _concurrencyScopes = materializedConcurrencyScopes;
            _hasOrderedBatch = true;
            _currentLegacyEvents = null;
            _stagedEvents.Add(new OrderedStagedEvents(stagedEvents));
            foreach (var (scopeLabel, concurrencyScope) in resolution.Scopes)
            {
                EnrollStrictConcurrencyScope(scopeLabel, concurrencyScope);
            }
            CompleteEnrollment(resolution);
        }
    }

    /// <inheritdoc/>
    public void AddDecisionRead(IDecisionRead read)
    {
        ImmediateAppendGuard.RequireUnit(this);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ReadProtectionGuard.ProtectedRead(this, read.ReadModelType.Name);
            if (IsEnforcingReadProtection)
            {
                AddEnforcedDecisionRead(read);
                return;
            }
            if (IsCompleted || _completing) throw new DecisionReadAfterCompletion();
            DecisionReadScopes.Validate(read, _eventStore.Value.Name, _eventStore.Value.Namespace, EventSequenceId.Log);
            ValidateLegacyEventSequenceIdsForOrderedBatch(EventSequenceId.Log);
            EnsureEventSequenceCanBeUsed(EventSequenceId.Log);
            var label = new EventSourceId(read.Scope.EventSourceId!.Value);
            if (_decisionScopes.TryGetValue(label, out var existing))
            {
                _decisionScopes[label] = DecisionReadScopes.Merge(existing, read.Scope);
            }
            else
            {
                if (_concurrencyScopes.Keys.Any(_ => _.Value == label.Value))
                {
                    throw new DecisionReadConflictsWithConcurrencyScope(label);
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
            .SelectMany(violation => _decisionConflicts.TryGetValue(violation.EventSourceId, out var conflicts) ? conflicts.Select(_ => _.CausedBy(violation)) : [])
            .Distinct().ToArray();

    /// <summary>Claims exclusive completion ownership for protected units of work.</summary>
    /// <returns>The opaque owner capability.</returns>
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">An owner has already claimed this unit.</exception>
    public DecisionReadCommitOwner ClaimDecisionReadCommitOwnership()
    {
        ReadProtectionGuard.RequireUnit(this);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfUnitOfWorkIsCompleted();
            RequireImmediateAppendOwner(null);
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
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            if (!ReferenceEquals(owner, _commitOwner)) throw new ProtectedUnitOfWorkRequiresOwner();
        }
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
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">A claimed unit with enrolled decision reads must be rolled back by its owner.</exception>
    /// <exception cref="UnitOfWorkIsCompleting">A commit is in progress under the strict policy, or a claimed unit with enrolled decision reads is committing under the compatibility policy.</exception>
    /// <exception cref="UnitOfWorkIsAlreadyCommitted">The unit has already been committed.</exception>
    /// <exception cref="UnitOfWorkIsAlreadyRolledBack">The unit has already been rolled back.</exception>
    public Task Rollback() => RollbackCore(null, false);

    /// <summary>Rolls back using the capability obtained by the transaction owner.</summary>
    /// <param name="owner">The claimed owner capability.</param>
    /// <returns>The rollback task.</returns>
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">The capability does not match the claimed owner, or no owner has been claimed.</exception>
    /// <exception cref="UnitOfWorkIsCompleting">A commit is in progress, regardless of lifecycle policy.</exception>
    /// <exception cref="UnitOfWorkIsAlreadyCommitted">The unit has already been committed.</exception>
    /// <exception cref="UnitOfWorkIsAlreadyRolledBack">The unit has already been rolled back.</exception>
    public Task RollbackAsOwner(DecisionReadCommitOwner owner) => RollbackCore(owner, true);

    /// <inheritdoc/>
    public void OnCompleted(Action<IUnitOfWork> callback)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            _onCompleted = callback;
        }
    }

    /// <inheritdoc/>
    public bool TryGetLastCommittedEventSequenceNumber([NotNullWhen(true)] out EventSequenceNumber? eventSequenceNumber)
    {
        eventSequenceNumber = _lastCommittedEventSequenceNumber;
        return eventSequenceNumber is not null;
    }

    /// <inheritdoc/>
    /// <remarks>Disposal during an in-flight commit is a no-op; the commit completes the unit.</remarks>
    /// <exception cref="ProtectedUnitOfWorkRequiresOwner">A claimed, open unit with enrolled decision reads must be completed by its owner.</exception>
    public void Dispose()
    {
        ReadProtectionGuard.RequireUnit(this);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            if (!IsCompleted) RequireImmediateAppendOwner(null);
            if (IsEnforcingReadProtection && !IsCompleted) throw new ProtectedUnitOfWorkRequiresOwner();
            if (_completing || IsCompleted) return;
            if (_commitOwner is not null && _hasEnrolledDecisionReads) throw new ProtectedUnitOfWorkRequiresOwner();

            using var span = _activitySource.Rollback(CorrelationId.ToString(), SpanNames);
            CompleteRollback();
        }
        _onCompleted(this);
    }

    /// <summary>
    /// Gets a value indicating whether the unit was completed by rolling it back rather than committing it.
    /// </summary>
    /// <returns><see langword="true"/> if the unit was rolled back; otherwise <see langword="false"/>.</returns>
    internal bool WasRolledBack() => _isRolledBack;

    /// <summary>
    /// Gets the exception a commit of this unit threw, when it threw rather than returning a result.
    /// </summary>
    /// <returns>The exception, or <see langword="null"/> if the commit returned normally or has not run.</returns>
    /// <remarks>
    /// A commit that throws still completes the unit, so whether events were appended is unknown; its
    /// <see cref="IsSuccess"/> must not be read as the outcome.
    /// </remarks>
    internal Exception? CommitFailure()
    {
        lock (_decisionLock)
        {
            return _commitFailure;
        }
    }

    static bool ConcurrencyScopesAreSemanticallyEqual(ConcurrencyScope first, ConcurrencyScope second)
    {
        return first.SequenceNumber == second.SequenceNumber && DefinitionConcurrencyScopeResolver.SamePredicate(first, second);
    }

    static ConcurrencyScope MaterializeConcurrencyScope(ConcurrencyScope concurrencyScope) =>
        concurrencyScope.EventTypes is null
            ? concurrencyScope
            : concurrencyScope with { EventTypes = concurrencyScope.EventTypes.ToArray() };

    static Dictionary<EventSourceId, List<ConcurrencyScope>> MaterializeConcurrencyScopes(
        IReadOnlyDictionary<EventSourceId, List<ConcurrencyScope>> concurrencyScopes) =>
        concurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value.ConvertAll(MaterializeConcurrencyScope));

    static void ThrowIfLabelIsNotSpecified(EventSourceId label)
    {
        if (label == EventSourceId.Unspecified || string.IsNullOrWhiteSpace(label.Value))
        {
            throw new ConcurrencyScopeLabelMustBeSpecified();
        }
    }

    static void ThrowIfScopesConflict(EventSourceId scopeLabel, ConcurrencyScope enrolledScope, ConcurrencyScope concurrencyScope)
    {
        if (DisagreeOnNone(enrolledScope, concurrencyScope) || HasSamePredicateButDiffers(enrolledScope, concurrencyScope))
        {
            throw new ConflictingConcurrencyScopesForLabel(scopeLabel, enrolledScope, concurrencyScope);
        }
    }

    static bool DisagreeOnNone(ConcurrencyScope first, ConcurrencyScope second) =>
        first == ConcurrencyScope.None != (second == ConcurrencyScope.None);

    static bool HasSamePredicateButDiffers(ConcurrencyScope first, ConcurrencyScope second) =>
        DefinitionConcurrencyScopeResolver.SamePredicate(first, second) && !ConcurrencyScopesAreSemanticallyEqual(first, second);

    bool RequiresRollbackOwner() => IsEnforcingReadProtection || HasClaimedDecisionReads;

    bool IsClaimedBy(DecisionReadCommitOwner? owner) => _commitOwner is not null && ReferenceEquals(owner, _commitOwner);

    Task RollbackCore(DecisionReadCommitOwner? owner, bool fromOwner, ImmediateAppendRefusal? appendOwner = null)
    {
        ReadProtectionGuard.RequireUnit(this);
        using var span = _activitySource.Rollback(CorrelationId.ToString(), SpanNames);

        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            RequireImmediateAppendOwner(appendOwner);
            ThrowIfRollbackOwnerIsRequired(owner, fromOwner, appendOwner);
            ThrowIfUnitOfWorkIsCompleted();
            ThrowIfRollbackIsBlocked(fromOwner);
            CompleteRollback();
        }

        _onCompleted(this);
        return Task.CompletedTask;
    }

    void ThrowIfRollbackOwnerIsRequired(DecisionReadCommitOwner? owner, bool fromOwner, ImmediateAppendRefusal? appendOwner)
    {
        if (appendOwner is null && fromOwner && !IsClaimedBy(owner)) throw new ProtectedUnitOfWorkRequiresOwner();
    }

    void ThrowIfRollbackIsBlocked(bool fromOwner)
    {
        if (_completing && RequiresOwnedRollback(fromOwner)) throw new UnitOfWorkIsCompleting(CorrelationId);
        if (RequiresRollbackOwner() && !fromOwner) throw new ProtectedUnitOfWorkRequiresOwner();
    }

    bool RequiresOwnedRollback(bool fromOwner) => IsCompletionStrict || fromOwner || HasClaimedDecisionReads;

    void CompleteRollback()
    {
        _isRolledBack = true;
        _stagedEvents.Clear();
        _currentLegacyEvents = null;
        _concurrencyScopes.Clear();
        _effectKey = null;
        _effectReceipt = null;
        _requiredEnrollmentScopes.Clear();
        _enrollmentContributors.Clear();
        _appendManyResult = AppendManyResult.Success(CorrelationId.NotSet, []);
    }

    void ThrowIfStagingAfterCompletion()
    {
        ImmediateAppendGuard.RequireUnit(this);
        ReadProtectionGuard.RequireUnit(this);
        ThrowIfMutatedDuringEnrollment();
        ThrowIfEnrollmentNotificationFailed();
        if (!_completing && !IsCompleted) return;

        if (IsCompletionStrict)
        {
            throw new UnitOfWorkIsCompleted(CorrelationId);
        }

        if (_logger is not null)
        {
            _logger.LateStagingAttemptedAfterCompletion(CorrelationId);
        }
        else
        {
            Trace.TraceError("Late event staging was attempted after unit of work '{0}' began completing; these events will not be appended by this unit of work.", CorrelationId);
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

        if (!_concurrencyScopes.TryGetValue(scopeLabel, out var scopes)) _concurrencyScopes[scopeLabel] = scopes = [];
        if (concurrencyScope != ConcurrencyScope.None && scopes.TrueForAll(_ => _ == ConcurrencyScope.None)) scopes.Clear();
        var index = scopes.FindIndex(_ => DefinitionConcurrencyScopeResolver.SamePredicate(_, concurrencyScope));
        if (index < 0) scopes.Add(concurrencyScope);
        else scopes[index] = concurrencyScope;
    }

    void EnrollStrictConcurrencyScope(EventSourceId scopeLabel, ConcurrencyScope concurrencyScope)
    {
        if (concurrencyScope == ConcurrencyScope.NotSet)
        {
            return;
        }

        if (!_concurrencyScopes.TryGetValue(scopeLabel, out var scopes)) _concurrencyScopes[scopeLabel] = scopes = [];
        if (!scopes.Exists(_ => RequiredScopesAreEqual(_, concurrencyScope))) scopes.Add(concurrencyScope);
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

        _eventSequence = _eventStore.Value.GetEventSequence(eventSequenceId);
        _eventSequenceId = eventSequenceId;
    }

    void BindLegacyEventSequence(EventSequenceId eventSequenceId)
    {
        var eventSequence = _eventStore.Value.GetEventSequence(eventSequenceId);
        _legacyEventSequenceIds.Add(eventSequenceId);
        if (_eventSequence is not null)
        {
            return;
        }

        _eventSequence = eventSequence;
        _eventSequenceId = eventSequenceId;
    }

    void ValidateExistingConcurrencyScopesForOrderedBatch(IReadOnlyDictionary<EventSourceId, List<ConcurrencyScope>> concurrencyScopes)
    {
        foreach (var (scopeLabel, concurrencyScope) in concurrencyScopes.SelectMany(_ => _.Value.Select(scope => (_.Key, Scope: scope))))
        {
            ThrowIfScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope);
        }
    }

    void ThrowIfScopeEventSourceIdDoesNotMatchLabel(EventSourceId scopeLabel, ConcurrencyScope concurrencyScope)
    {
        if (concurrencyScope.EventSourceId is not null && concurrencyScope.EventSourceId.Value != scopeLabel.Value)
        {
            throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope.EventSourceId);
        }
    }

    void ValidateConcurrencyScope(
        EventSequenceId eventSequenceId,
        EventSourceId scopeLabel,
        ConcurrencyScope concurrencyScope,
        Dictionary<EventSourceId, List<ConcurrencyScope>> enrolledConcurrencyScopes,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope>? requiredScopes = null)
    {
        ThrowIfScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope);

        if (concurrencyScope == ConcurrencyScope.NotSet || !enrolledConcurrencyScopes.TryGetValue(scopeLabel, out var enrolledScopes))
        {
            return;
        }

        foreach (var scope in enrolledScopes)
        {
            if (requiredScopes?.ContainsKey(scopeLabel) == true)
            {
                MergeRequiredScope(eventSequenceId, scopeLabel, scope, concurrencyScope);
            }
            else
            {
                ThrowIfScopesConflict(scopeLabel, scope, concurrencyScope);
            }
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
