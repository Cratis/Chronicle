// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions;

public partial class UnitOfWork
{
    static bool HasNamedTags(EventForEventSourceId[] events) => events.Any(_ => _.NamedTags.Any());

    static bool IsValidateOnlyRejection(CommitBatch batch, CommandFailed exception) =>
        batch.ProtectedCommit &&
        batch.Events.Length == 0 &&
        exception.Result?.ValidationResults.Any(_ => _.Message == "At least one event is required.") == true;

    async Task CommitCore(bool fromOwner, ImmediateAppendRefusal? appendOwner = null)
    {
        ReadProtectionGuard.RequireUnit(this);
        using var span = _activitySource.Commit(CorrelationId.ToString(), SpanNames);

        var batch = CaptureCommitBatch(fromOwner, appendOwner);

        try
        {
            await AppendCommitBatch(batch);
        }
        catch (Exception exception)
        {
            RecordCommitFailure(exception, batch, appendOwner);
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

    CommitBatch CaptureCommitBatch(bool fromOwner, ImmediateAppendRefusal? appendOwner)
    {
        lock (_decisionLock)
        {
            ThrowIfMutatedDuringEnrollment();
            ThrowIfUnitOfWorkIsCompleted();
            ThrowIfEnrollmentNotificationFailed();
            RequireImmediateAppendOwner(appendOwner);
            if (_completing) throw new DecisionReadAfterCompletion();
            var protectedCommit = IsEnforcingReadProtection || _decisionScopes.Count != 0;
            if (protectedCommit && appendOwner is null && (!fromOwner || _commitOwner is null)) throw new ProtectedUnitOfWorkRequiresOwner();
            _completing = true;

            // Capture the commit batch under the same lock as enrollment. Compatibility calls can
            // still stage after this point, but they must never enter the in-flight append.
            var eventSequence = _eventSequence;
            var events = eventSequence is null ? [] : GetEventsToCommit();
            var scopes = CaptureCommitScopes(protectedCommit, events);
            return new(eventSequence, events, scopes, protectedCommit, CaptureReadProtectionCommit());
        }
    }

    KeyValuePair<EventSourceId, ConcurrencyScope>[] CaptureCommitScopes(bool protectedCommit, EventForEventSourceId[] events)
    {
        var enrolled = _concurrencyScopes.SelectMany(_ => _.Value.Select(scope => new KeyValuePair<EventSourceId, ConcurrencyScope>(_.Key, scope)));
        return !protectedCommit ? enrolled.ToArray() : enrolled.Concat(DecisionReadScopes.ForEvents(_decisionScopes, events)).ToArray();
    }

    ReadProtectionCommit? CaptureReadProtectionCommit() =>
        IsEnforcingReadProtection
            ? new(GetReadProtectionTarget(), Array.AsReadOnly(_readProtectionContributions.ToArray()), CaptureAppendConditions) { ValidateReadsWithoutEvents = _effectReceipt is not null }
            : null;

    async Task AppendCommitBatch(CommitBatch batch)
    {
        SkipAppendForSatisfiedReadsWithoutEvents(batch);
        if (_effectReceipt is not null) EffectReceiptSupport.Require(_eventStore.Value.Connection);
        if (ShouldAppend(batch))
        {
            RecordAppendResult(await SendAppend(batch));
        }
        else if (_effectReceipt is not null)
        {
            var response = _effectReceipt.Response is null ? null : JsonNode.Parse(_effectReceipt.Response);
            EffectReceipt = await RecordNoEventsReceipt(response);
        }
        if (IsEnforcingReadProtection && _appendManyResult.IsSuccess) _readProtectionFrameLease!().CommittedSuccessfully();
    }

    void SkipAppendForSatisfiedReadsWithoutEvents(CommitBatch batch)
    {
        if (batch.ReadProtectionCommit is null || batch.Events.Length != 0) return;

        var readProtectionCommit = batch.ReadProtectionCommit;
        var plan = ReadProtectionMerge.Apply(readProtectionCommit.Target, readProtectionCommit.Contributions, batch.Scopes, false, readProtectionCommit.ValidateReadsWithoutEvents);
        if (plan.Scopes.Count != 0) return;

        CaptureAppendConditions(plan.Conditions);
        batch.EventSequence = null;
    }

    bool ShouldAppend(CommitBatch batch) =>
        batch.EventSequence is not null &&
        (_effectReceipt is null || batch.Events.Length != 0 || batch.Scopes.Length != 0 || batch.ReadProtectionCommit?.Contributions.Count > 0);

    async Task<AppendManyResult> SendAppend(CommitBatch batch)
    {
        try
        {
            return await DispatchAppend(batch.EventSequence!, batch);
        }
        catch (CommandFailed exception) when (IsValidateOnlyRejection(batch, exception))
        {
            throw new DecisionReadValidateOnlyNotSupported();
        }
    }

    Task<AppendManyResult> DispatchAppend(IEventSequence eventSequence, CommitBatch batch)
    {
        var events = batch.Events;
        var scopes = batch.Scopes;
        if (_effectReceipt is not null)
        {
            return eventSequence is IEventSequenceWithEffectReceipts receipted
                ? receipted.AppendManyWithEffectReceipt(events, scopes, _effectReceipt, batch.ReadProtectionCommit)
                : throw new EffectReceiptsNotSupported();
        }

        if (batch.ReadProtectionCommit is not null)
        {
            return eventSequence is IEventSequenceWithReadProtection supported
                ? supported.AppendMany(events, [], scopes, batch.ReadProtectionCommit)
                : throw new EventSequenceReadProtectionNotSupported(eventSequence.GetType());
        }

        if (_immediateAppendRefusalLease is not null)
        {
            return eventSequence is EventSequence native
                ? native.AppendManyForUnitOfWork(events, scopes)
                : throw new EventSequenceImmediateAppendRefusalNotSupported(eventSequence.GetType());
        }

        return scopes.GroupBy(_ => _.Key).Any(_ => _.Count() > 1)
            ? AppendWithRepeatedScopeLabels(eventSequence, events, scopes)
            : AppendWithSingleScopes(eventSequence, events, scopes);
    }

    Task<AppendManyResult> AppendWithRepeatedScopeLabels(
        IEventSequence eventSequence,
        EventForEventSourceId[] events,
        KeyValuePair<EventSourceId, ConcurrencyScope>[] scopes) =>
        HasNamedTags(events)
            ? eventSequence.AppendManyWithNamedTags(events, [], null, null, scopes)
            : eventSequence.AppendMany(events, null, null, scopes);

    Task<AppendManyResult> AppendWithSingleScopes(
        IEventSequence eventSequence,
        EventForEventSourceId[] events,
        KeyValuePair<EventSourceId, ConcurrencyScope>[] scopes)
    {
        var singleScopes = scopes.ToDictionary(_ => _.Key, _ => _.Value);
        return HasNamedTags(events)
            ? eventSequence.AppendManyWithNamedTags(events, [], concurrencyScopes: singleScopes)
            : eventSequence.AppendMany(events, concurrencyScopes: singleScopes);
    }

    void RecordAppendResult(AppendManyResult result)
    {
        if (result.SequenceNumbers?.Any() == true)
        {
            _lastCommittedEventSequenceNumber = result.SequenceNumbers.MaxBy(_ => _.Value);
        }
        _appendManyResult = result;
        if (_effectReceipt is null) return;

        EffectReceipt = result.EffectReceipt;
        if (result.IsSuccess && EffectReceipt is null) throw new EffectReceiptNotValid("A successful atomic append did not return its receipt.");
    }

    void RecordCommitFailure(Exception exception, CommitBatch batch, ImmediateAppendRefusal? appendOwner)
    {
        lock (_decisionLock)
        {
            _commitFailure = exception;
        }
        if (appendOwner is not null || batch.ProtectedCommit || _effectReceipt is not null)
        {
            _appendManyResult = AppendManyResult.Failed(CorrelationId, [new AppendError(exception.Message)]);
        }
    }

    Task<EffectReceiptResult> RecordNoEventsReceipt(JsonNode? response)
    {
        if (!IsEnforcingReadProtection) return _eventStore.Value.Occurrences.RecordEffectReceipt(_effectKey!, new NoEvents(), response);

        return _eventStore.Value.Occurrences is IOccurrencesForUnitOfWork trusted
            ? trusted.RecordNoEvents(_effectKey!, response)
            : throw new EffectReceiptsNotSupported();
    }

    sealed class CommitBatch(
        IEventSequence? eventSequence,
        EventForEventSourceId[] events,
        KeyValuePair<EventSourceId, ConcurrencyScope>[] scopes,
        bool protectedCommit,
        ReadProtectionCommit? readProtectionCommit)
    {
        public IEventSequence? EventSequence { get; set; } = eventSequence;

        public EventForEventSourceId[] Events { get; } = events;

        public KeyValuePair<EventSourceId, ConcurrencyScope>[] Scopes { get; } = scopes;

        public bool ProtectedCommit { get; } = protectedCommit;

        public ReadProtectionCommit? ReadProtectionCommit { get; } = readProtectionCommit;
    }
}
