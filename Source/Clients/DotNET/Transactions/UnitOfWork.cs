// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Contracts.Commands;
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
/// <param name="correlationId">The <see cref="CorrelationId"/> for the <see cref="IUnitOfWork"/>.</param>
/// <param name="onCompleted">The action to call when the <see cref="IUnitOfWork"/> is completed.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to use for the <see cref="IUnitOfWork"/>.</param>
/// <param name="activitySource">Optional <see cref="IActivitySource{T}"/> for tracing. Defaults to a source named <see cref="ClientActivity.SourceName"/> when not provided.</param>
/// <param name="lifecyclePolicy">The policy for staging events once completion begins.</param>
/// <param name="logger">Optional logger for late staging diagnostics.</param>
public partial class UnitOfWork(
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
        new ActivitySource<UnitOfWork>(new ActivitySource(ClientActivity.SourceName));

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
    EffectKey? _effectKey;
    Contracts.Occurrences.EffectReceipt? _effectReceipt;

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

    /// <inheritdoc/>
    public void UseEffectReceipt(EffectKey key, JsonNode? response = null)
    {
        ReadProtectionGuard.RequireUnit(this);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing) throw new UnitOfWorkIsCompleting(correlationId);
            EffectReceiptSupport.Require(eventStore.Connection);
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
            if (IsEnforcingReadProtection)
            {
                AddEnforcedEvents(eventSequenceId, events, concurrencyScopes, origin);
                return;
            }
            var suppliedScopes = concurrencyScopes.Select(pair => new KeyValuePair<EventSourceId, ConcurrencyScope>(
                pair.Key, MaterializeConcurrencyScope(pair.Value))).ToArray();
            var batch = new EventsWithConcurrencyScopes(events, suppliedScopes.Select(pair =>
                _requiredEnrollmentScopes.TryGetValue(pair.Key, out var required)
                    ? new KeyValuePair<EventSourceId, ConcurrencyScope>(pair.Key, MergeRequiredScope(eventSequenceId, pair.Key, pair.Value, required))
                    : pair));
            var targets = batch.Events.GroupBy(_ => _.EventSourceId).ToDictionary(_ => _.Key, _ => Array.AsReadOnly(_.Select(e => e.Event).ToArray()));
            var enrollments = targets.Keys.Union(batch.ConcurrencyScopes.Keys).Select(source => new UnitOfWorkEnrollment(
                eventSequenceId,
                source,
                suppliedScopes.FirstOrDefault(_ => _.Key == source).Value ?? ConcurrencyScope.NotSet,
                targets.TryGetValue(source, out var sourceEvents) ? sourceEvents : Array.AsReadOnly(Array.Empty<object>()))).ToArray();
            var resolution = ResolveEnrollmentScopes(eventSequenceId, enrollments);
            foreach (var (label, scope) in resolution.Scopes)
            {
                if (scope != ConcurrencyScope.NotSet && _decisionScopes.ContainsKey(new(label.Value)))
                {
                    throw new DecisionReadConflictsWithConcurrencyScope(label);
                }
            }
            ValidateLegacyEventSequenceIdsForOrderedBatch(eventSequenceId);
            EnsureEventSequenceCanBeUsed(eventSequenceId);
            ValidateExistingEventTargetsForOrderedBatch();
            var materializedConcurrencyScopes = MaterializeConcurrencyScopes(_concurrencyScopes);
            ValidateExistingConcurrencyScopesForOrderedBatch(materializedConcurrencyScopes);
            foreach (var (label, scope) in resolution.Scopes)
            {
                ValidateConcurrencyScope(eventSequenceId, label, scope, materializedConcurrencyScopes, resolution.Requirements);
            }
            var stagedEvents = batch.Events.Select(_ => _ with
            {
                NamedTags = NamedTagConverters.Merge(_.NamedTags, [])
            }).ToArray();

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
            DecisionReadScopes.Validate(read, eventStore.Name, eventStore.Namespace, EventSequenceId.Log);
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
            .SelectMany(_ => _decisionConflicts.TryGetValue(_.EventSourceId, out var conflicts) ? conflicts : [])
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
            if (IsEnforcingReadProtection && !IsCompleted) throw new ProtectedUnitOfWorkRequiresOwner();
            if (_completing || IsCompleted) return;
            if (_commitOwner is not null && _hasEnrolledDecisionReads) throw new ProtectedUnitOfWorkRequiresOwner();

            using var span = _activitySource.Rollback(correlationId.ToString());
            CompleteRollback();
        }
        _onCompleted(this);
    }

    static bool ConcurrencyScopesAreSemanticallyEqual(ConcurrencyScope first, ConcurrencyScope second)
    {
        if (first.SequenceNumber != second.SequenceNumber ||
            first.EventSourceId?.Value != second.EventSourceId?.Value ||
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

    Task RollbackCore(DecisionReadCommitOwner? owner, bool fromOwner)
    {
        ReadProtectionGuard.RequireUnit(this);
        using var span = _activitySource.Rollback(correlationId.ToString());

        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            if (fromOwner && (_commitOwner is null || !ReferenceEquals(owner, _commitOwner))) throw new ProtectedUnitOfWorkRequiresOwner();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing)
            {
                if (IsEnforcingReadProtection || lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict) throw new UnitOfWorkIsCompleting(correlationId);
                if (fromOwner || (_commitOwner is not null && _hasEnrolledDecisionReads)) throw new UnitOfWorkIsCompleting(correlationId);
            }
            if ((IsEnforcingReadProtection || (_commitOwner is not null && _hasEnrolledDecisionReads)) && !fromOwner) throw new ProtectedUnitOfWorkRequiresOwner();
            CompleteRollback();
        }

        _onCompleted(this);
        return Task.CompletedTask;
    }

    async Task CommitCore(bool fromOwner)
    {
        ReadProtectionGuard.RequireUnit(this);
        using var span = _activitySource.Commit(correlationId.ToString());

        Dictionary<EventSourceId, ConcurrencyScope> scopes;
        EventForEventSourceId[] events;
        IEventSequence? eventSequence;
        bool protectedCommit;
        ReadProtectionCommit? readProtectionCommit;
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfUnitOfWorkIsCompleted();
            ThrowIfEnrollmentNotificationFailed();
            if (_completing) throw new DecisionReadAfterCompletion();
            protectedCommit = IsEnforcingReadProtection || _decisionScopes.Count != 0;
            if (protectedCommit && (!fromOwner || _commitOwner is null)) throw new ProtectedUnitOfWorkRequiresOwner();
            _completing = true;

            // Capture the commit batch under the same lock as enrollment. Compatibility calls can
            // still stage after this point, but they must never enter the in-flight append.
            eventSequence = _eventSequence;
            events = eventSequence is null ? [] : GetEventsToCommit();
            scopes = !protectedCommit ? _concurrencyScopes.ToDictionary(_ => _.Key, _ => _.Value) :
                _concurrencyScopes.Concat(DecisionReadScopes.ForEvents(_decisionScopes, events)).ToDictionary(_ => _.Key, _ => _.Value);
            readProtectionCommit = IsEnforcingReadProtection
                ? new(GetReadProtectionTarget(), Array.AsReadOnly(_readProtectionContributions.ToArray()), CaptureAppendConditions) { ValidateReadsWithoutEvents = _effectReceipt is not null }
                : null;
        }

        try
        {
            if (readProtectionCommit is not null && events.Length == 0)
            {
                var plan = ReadProtectionMerge.Apply(readProtectionCommit.Target, readProtectionCommit.Contributions, scopes, false, readProtectionCommit.ValidateReadsWithoutEvents);
                CaptureAppendConditions(plan.Conditions);
                if (plan.Scopes.Count == 0) eventSequence = null;
            }
            if (_effectReceipt is not null) EffectReceiptSupport.Require(eventStore.Connection);
            if (eventSequence is not null && (_effectReceipt is null || events.Length != 0 || scopes.Count != 0 || readProtectionCommit?.Contributions.Count > 0))
            {
                AppendManyResult result;
                try
                {
                    if (_effectReceipt is not null)
                    {
                        result = eventSequence is IEventSequenceWithEffectReceipts receipted
                            ? await receipted.AppendManyWithEffectReceipt(events, scopes, _effectReceipt, readProtectionCommit)
                            : throw new EffectReceiptsNotSupported();
                    }
                    else if (readProtectionCommit is not null)
                    {
                        result = eventSequence is IEventSequenceWithReadProtection supported
                            ? await supported.AppendMany(events, [], scopes, readProtectionCommit)
                            : throw new EventSequenceReadProtectionNotSupported(eventSequence.GetType());
                    }
                    else
                    {
                        result = events.Any(_ => _.NamedTags.Any())
                            ? await eventSequence.AppendManyWithNamedTags(events, [], concurrencyScopes: scopes)
                            : await eventSequence.AppendMany(events, concurrencyScopes: scopes);
                    }
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
                if (_effectReceipt is not null)
                {
                    EffectReceipt = result.EffectReceipt;
                    if (result.IsSuccess && EffectReceipt is null) throw new EffectReceiptNotValid("A successful atomic append did not return its receipt.");
                }
            }
            else if (_effectReceipt is not null)
            {
                var response = _effectReceipt.Response is null ? null : JsonNode.Parse(_effectReceipt.Response);
                EffectReceipt = await RecordNoEventsReceipt(response);
            }
            if (IsEnforcingReadProtection && _appendManyResult.IsSuccess) _readProtectionFrameLease!().CommittedSuccessfully();
        }
        catch (Exception exception) when (protectedCommit || _effectReceipt is not null)
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

    Task<EffectReceiptResult> RecordNoEventsReceipt(JsonNode? response)
    {
        if (!IsEnforcingReadProtection) return eventStore.Occurrences.RecordEffectReceipt(_effectKey!, new NoEvents(), response);

        return eventStore.Occurrences is IOccurrencesForUnitOfWork trusted
            ? trusted.RecordNoEvents(_effectKey!, response)
            : throw new EffectReceiptsNotSupported();
    }

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
        ReadProtectionGuard.RequireUnit(this);
        ThrowIfMutatedDuringEnrollment();
        ThrowIfEnrollmentNotificationFailed();
        if (!_completing && !IsCompleted) return;

        if (IsEnforcingReadProtection || lifecyclePolicy == UnitOfWorkLifecyclePolicy.Strict)
        {
            throw new UnitOfWorkIsCompleted(correlationId);
        }

        if (_logger is not null)
        {
            _logger.LateStagingAttemptedAfterCompletion(correlationId);
        }
        else
        {
            Trace.TraceError("Late event staging was attempted after unit of work '{0}' began completing; these events will not be appended by this unit of work.", correlationId);
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

    void ValidateExistingConcurrencyScopesForOrderedBatch(IReadOnlyDictionary<EventSourceId, ConcurrencyScope> concurrencyScopes)
    {
        foreach (var (scopeLabel, concurrencyScope) in concurrencyScopes)
        {
            if (concurrencyScope.EventSourceId is not null && concurrencyScope.EventSourceId.Value != scopeLabel.Value)
            {
                throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope.EventSourceId);
            }
        }
    }

    void ValidateConcurrencyScope(
        EventSequenceId eventSequenceId,
        EventSourceId scopeLabel,
        ConcurrencyScope concurrencyScope,
        Dictionary<EventSourceId, ConcurrencyScope> enrolledConcurrencyScopes,
        IReadOnlyDictionary<EventSourceId, ConcurrencyScope>? requiredScopes = null)
    {
        if (concurrencyScope.EventSourceId is not null && concurrencyScope.EventSourceId.Value != scopeLabel.Value)
        {
            throw new ConcurrencyScopeEventSourceIdDoesNotMatchLabel(scopeLabel, concurrencyScope.EventSourceId);
        }

        if (concurrencyScope == ConcurrencyScope.NotSet || !enrolledConcurrencyScopes.TryGetValue(scopeLabel, out var enrolledScope))
        {
            return;
        }

        if (requiredScopes?.ContainsKey(scopeLabel) == true)
        {
            MergeRequiredScope(eventSequenceId, scopeLabel, enrolledScope, concurrencyScope);
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
