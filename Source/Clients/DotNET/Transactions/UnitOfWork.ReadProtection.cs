// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    readonly List<ReadProtectionContribution> _readProtectionContributions = [];
    IReadOnlyList<AppendCondition> _appendConditions = [];

    /// <summary>
    /// The pipeline owns disposal; the unit retains only access to non-owning leases.
    /// </summary>
    Func<ReadProtectionFrame>? _readProtectionFrameLease;

    /// <inheritdoc/>
    public bool IsEnforcingReadProtection => _readProtectionFrameLease is not null;

    /// <inheritdoc/>
    /// <exception cref="ReadProtectionMustBeginBeforeEnrollment">Events or dependencies have already been enrolled.</exception>
    public ReadProtectionFrame EnforceReadProtection()
    {
        ReadProtectionGuard.RequireUnit(this, false);
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfEnrollmentNotificationFailed();
            if (_readProtectionFrameLease is not null) return _readProtectionFrameLease();
            ThrowIfUnitOfWorkIsCompleted();
            if (_completing || _stagedEvents.Count != 0 || _concurrencyScopes.Count != 0 || _hasEnrolledDecisionReads)
            {
                throw new ReadProtectionMustBeginBeforeEnrollment();
            }
            var frame = new ReadProtectionFrame(this, () => eventStore.GetEventSequence(_eventSequenceId ?? EventSequenceId.Log), GetReadProtectionTarget);
            _readProtectionFrameLease = frame.Borrow;
            return frame;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ReadProtectionAllowanceNotValid">The allowance or reason is invalid.</exception>
    public IDisposable AllowReads(ReadAllowance allowance, string reason)
    {
        ReadProtectionGuard.RequireUnit(this);
        if (allowance is not ReadAllowance.Advisory and not ReadAllowance.Contributed || string.IsNullOrWhiteSpace(reason)) throw new ReadProtectionAllowanceNotValid();
        return new ReadProtectionReadRegion(allowance, reason);
    }

    /// <inheritdoc/>
    /// <exception cref="ReadProtectionUnitMismatch">The unit does not own the active invocation.</exception>
    public IDisposable SuspendReadRegions()
    {
        ReadProtectionGuard.RequireUnit(this);
        return ReadProtectionReadRegion.Suspend("Host suspended inherited read regions");
    }

    /// <inheritdoc/>
    /// <exception cref="ContributedReadOriginNotValid">The contribution is not explicit or aggregate.</exception>
    public void EnrolContributedRead(ContributedRead contribution)
    {
        ReadProtectionGuard.ProtectedRead(this, contribution.Description);
        if (contribution.Origin is not ConcurrencyScopeOrigin.Explicit and not ConcurrencyScopeOrigin.Aggregate) throw new ContributedReadOriginNotValid(contribution.Origin);
        lock (_decisionLock)
        {
            ThrowIfReadEnrollmentAfterCompletion(contribution.Description);
            var target = new ReadProtectionTarget(contribution.EventStore, contribution.Namespace, contribution.EventSequenceId);
            var kind = contribution.Origin == ConcurrencyScopeOrigin.Aggregate ? ReadProtectionContributionKind.Aggregate : ReadProtectionContributionKind.Explicit;
            var item = new ReadProtectionContribution(contribution.Label, kind, contribution.Scope, target, contribution.Description, ReadProtectionContributionIntent.ReadEnrolled);
            if (!IsEnforcingReadProtection)
            {
                ValidateReadProtectionTarget(item);
                AddEvents(contribution.EventSequenceId, [], [new(contribution.Label, contribution.Scope)], contribution.Origin);
                return;
            }
            EnrolProtectedContribution(item);
            BindToEventSequence(contribution.EventSequenceId);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<AppendCondition> GetAppendConditions()
    {
        lock (_decisionLock)
        {
            return _appendConditions;
        }
    }

    static void ValidateDistinctReadProtectionContribution(ReadProtectionContribution contribution, IEnumerable<ReadProtectionContribution> existing)
    {
        var onLabel = existing.Where(_ => _.Label == contribution.Label).Append(contribution).ToArray();
        var read = onLabel.FirstOrDefault(_ => _.Kind == ReadProtectionContributionKind.ProtectedRead);
        var aggregate = onLabel.FirstOrDefault(_ => _.Kind == ReadProtectionContributionKind.Aggregate);
        if (read is not null && aggregate is not null) throw new ReadAndAggregateOnSameSource(contribution.Label, read.Description, aggregate.Description);
        var exact = ReadProtectionMerge.Distinct(onLabel.Where(_ => _.Kind is ReadProtectionContributionKind.Explicit or ReadProtectionContributionKind.Aggregate));
        if (exact.Count <= 1) return;
        if (read is not null) throw new ConflictingExplicitScopes(contribution.Label, exact[0].Description, exact[1].Description);
        throw new ConflictingContributionsOnLabel(contribution.Label, exact[0].Description, exact[1].Description);
    }

    void CaptureAppendConditions(IReadOnlyList<AppendCondition> conditions)
    {
        lock (_decisionLock)
        {
            _appendConditions = conditions;
        }
    }

    ReadProtectionTarget GetReadProtectionTarget() => new(eventStore.Name, eventStore.Namespace, _eventSequenceId ?? EventSequenceId.Log);

    void AddEnforcedDecisionRead(IDecisionRead read)
    {
        ThrowIfReadEnrollmentAfterCompletion(read.ReadModelType.Name);
        if (!read.IsProtected) throw new UnprotectedDecisionRead();
        var label = new EventSourceId(read.Scope.EventSourceId?.Value ?? EventSourceId.Unspecified.Value);
        var item = new ReadProtectionContribution(label, ReadProtectionContributionKind.ProtectedRead, read.Scope, new(read.EventStore, read.Namespace, read.EventSequenceId), $"{read.ReadModelType.Name} '{read.Key}'", ReadProtectionContributionIntent.ReadEnrolled);
        ReadProtectionMerge.ValidateTargetsAndLabels(new(eventStore.Name, eventStore.Namespace, EventSequenceId.Log), [item]);
        EnrolProtectedContribution(item);
        BindToEventSequence(EventSequenceId.Log);
        if (!_decisionConflicts.TryGetValue(label, out var conflicts))
        {
            conflicts = [];
            _decisionConflicts.Add(label, conflicts);
        }
        var conflict = new DecisionConflict(read.ReadModelType, read.Key);
        if (!conflicts.Contains(conflict)) conflicts.Add(conflict);
        _hasEnrolledDecisionReads = true;
    }

    void EnrolProtectedContribution(ReadProtectionContribution contribution)
    {
        ValidateReadProtectionTarget(contribution);
        if (contribution.Scope.EventSourceId is null) throw new SourceLessReadNotSupported(contribution.Description);
        if (_stagedEvents.SelectMany(_ => _.GetEvents()).Any(@event => OverlapsStagedEvent(contribution.Scope, @event)))
        {
            throw new ReadOfSourceWithStagedEvents(contribution.Label, contribution.Description);
        }
        ValidateDistinctReadProtectionContribution(contribution, _readProtectionContributions);
        _readProtectionContributions.Add(contribution);
    }

    bool OverlapsStagedEvent(ConcurrencyScope scope, EventForEventSourceId @event)
    {
        var routing = @event.EventSource is null
            ? null
            : ResolvedEventRouting.Resolve(eventStore.EventSources, @event.EventSource, @event.EventStream, @event.EventSourceType, @event.EventStreamType);
        var effectiveEvent = ResolvedEventRouting.ResolveMetadata(@event, routing);
        return ReadProtectionPredicate.Matches(scope, effectiveEvent, eventStore.EventTypes.GetEventTypeFor(@event.Event.GetType()));
    }

    void ValidateReadProtectionTarget(ReadProtectionContribution contribution)
    {
        var sequence = _eventSequenceId ?? contribution.Target.EventSequence;
        ReadProtectionMerge.ValidateTargetsAndLabels(new(eventStore.Name, eventStore.Namespace, sequence), [contribution]);
        EnsureEventSequenceCanBeUsed(contribution.Target.EventSequence);
    }

    void ThrowIfReadEnrollmentAfterCompletion(string description)
    {
        ThrowIfMutatedDuringEnrollment();
        ThrowIfEnrollmentNotificationFailed();
        if (IsCompleted || _completing) throw new ReadAfterProtectedCommit(description);
    }
}
