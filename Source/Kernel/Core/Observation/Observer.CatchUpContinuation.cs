// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly SortedSet<EventSequenceNumber> _gatedLiveEvents = [];
    IGrainTimer? _catchUpContinuationTimer;
    JobId? _unboundCatchUpConclusion;
    bool _catchUpContinuationPending;
    bool _catchUpContinuationInProgress;
    bool _joinedCatchUp;
    bool _catchUpReloadRequired;
    bool _joinRunSettled;
    int _catchUpContinuationVersion;
    int _episodeTransfersInFlight;

    bool PartitionIsCatchingUp(Key partition) => State.CatchingUpPartitions.Contains(partition);

    bool CatchUpAdmissionClosed => _catchUpReloadRequired || _catchUpConclusionsInFlight > 0 || _episodeTransfersInFlight > 0 || HasAcceptedCatchUpActivity;

    /// <summary>
    /// Gets whether the catch-up continuation owns the check for backlog after subscribing.
    /// </summary>
    internal bool IsJoiningCatchUp => HasAcceptedCatchUpActivity;

    bool HasAcceptedCatchUpActivity => _catchUpContinuationPending || _catchUpContinuationInProgress;
    bool CatchUpAcceptanceInvalidated => _catchUpReloadRequired || _disposed || CatchUpObserverRemoved || !_observerExists || !KeyOwnsDefinition;

    async Task ConfirmCatchUpAcceptance()
    {
        try
        {
            // Admission is closed and in-flight markers pin the unsettled work. This write cannot
            // wait for a subscriber that might itself be waiting for the conclusion to return.
            if (!CatchUpAcceptanceInvalidated && await WriteStateAndConfirm(allowDuringDelivery: true)) return;
            throw new InvalidOperationException("The catch-up handover could not be durably accepted.");
        }
        catch
        {
            RequireCatchUpReload();
            throw;
        }
    }

    void ScheduleCatchUpContinuation()
    {
        _catchUpContinuationPending = true;
        _catchUpContinuationVersion++;
        _catchUpContinuationTimer ??= this.RegisterGrainTimer(
            async _ =>
            {
                _catchUpContinuationTimer?.Dispose();
                _catchUpContinuationTimer = null;
                await ContinueAcceptedCatchUp();
            },
            new GrainTimerCreationOptions { DueTime = TimeSpan.Zero, Period = Timeout.InfiniteTimeSpan, Interleave = false });
    }

    async Task ContinueAcceptedCatchUp()
    {
        if (!_catchUpContinuationPending || HasUnsettledCatchUpOwners() || !CanJoinSubscription()) return;

        var version = _catchUpContinuationVersion;
        _catchUpContinuationInProgress = true;
        try
        {
            await ReconcileCatchUpHolds(version);
            if (JoinStillCurrent(version) && await JoinLiveLane(version) && JoinStillCurrent(version))
            {
                _joinedCatchUp = true;
                _catchUpContinuationPending = false;
                _walkingEveryPartition = false;
                CancelShortCatchUpRetry();
                _catchupRecoveryAttempts = 0;
                _catchUpBackoffUntil = default;
            }
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            logger.ResumeFromDormancyFailed(exception);
        }
        finally
        {
            _catchUpContinuationInProgress = false;
        }
    }

    bool HasUnsettledCatchUpOwners() => _episodeTransfersInFlight > 0 || _catchUpContinuationInProgress ||
        _catchUpConclusionsInFlight > 0 || _catchUpHandoversInFlight > 0 || _pendingCatchUpAcquisition is not null;

    bool CanJoinSubscription() => !CatchUpAcceptanceInvalidated && !State.IsReplaying && !IsQuarantined && _subscription.IsSubscribed;

    async Task ReconcileCatchUpHolds(int version)
    {
        if (_unboundCatchUpConclusion is { } job)
        {
            if (await ShadowHandOverCaughtUpObserverWide(job) && JoinStillCurrent(version)) _unboundCatchUpConclusion = null;
        }

        var releasable = await PartitionHoldsReleasableAfterCatchUp();
        if (!JoinStillCurrent(version)) return;
        RewindUncoveredHoles();
        foreach (var partition in releasable)
        {
            State.CatchingUpPartitions.Remove(partition);
            ShadowHandOverPartition(partition, ClaimHolderKind.PartitionCatchUp);
        }
        _partitionHoldsVersion++;
    }

    async Task<bool> JoinLiveLane(int version)
    {
        await TransitionTo<Observing>();
        if (await GetCurrentState() is not Observing observing || !JoinStillCurrent(version)) return false;
        if (!await _appendedEventsQueues.IsSubscribed(_observerKey))
        {
            if (!JoinStillCurrent(version)) return false;
            await observing.SubscribeForJoin(State);
        }

        if (!JoinStillCurrent(version)) return false;
        RewindUncoveredHoles();
        var floor = State.NextEventSequenceNumber.IsActualValue ? State.NextEventSequenceNumber : EventSequenceNumber.First;
        if (!await CheckpointJoin(floor, version)) return false;
        var tail = await _eventSequence.GetTailSequenceNumber();
        if (!JoinStillCurrent(version)) return false;
        if (tail.IsActualValue && floor <= tail && !await WalkJoin(floor, tail, version)) return false;
        if (!JoinStillCurrent(version) || !await _appendedEventsQueues.IsSubscribed(_observerKey)) return false;

        // Handle and HandlePage are ordinary turns, like the continuation. Once it starts, live
        // dispatch blocks on the outstanding lane call; a spill closes that lane instead of dropping
        // through the gate. Only runs acknowledged before this turn need an explicit drain.
        return await DrainGatedLiveRuns(tail, version);
    }

    IEventSequenceStorage EventStorageForJoin() =>
        storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).GetEventSequence(_observerKey.EventSequenceId);

    bool JoinStillCurrent(int version) => version == _catchUpContinuationVersion && _episodeTransfersInFlight == 0 &&
        _catchUpConclusionsInFlight == 0 && CanJoinSubscription();

    async Task<bool> WalkJoin(EventSequenceNumber floor, EventSequenceNumber tail, int version)
    {
        var eventTypes = ReaderScope.IsWildcard ? null : ReaderScope.EventTypesToRead(_subscription.EventTypes)!
            .Append(new EventType(GlobalEventTypes.Redaction, EventTypeGeneration.First)).DistinctBy(type => type.Id).ToArray();
        var subscription = new AppendedEventsQueueObserverSubscription(_observerKey, _subscription.EventTypes, _subscription.Filters, ReaderScope.IsWildcard);
        using var cursor = await EventStorageForJoin().GetRange(floor, tail, eventTypes: eventTypes);
        while (await cursor.MoveNext())
        {
            var page = cursor.Current;
            if (!await SettleJoinPage(AppendedEventsQueue.SelectForSubscription(page, subscription), version)) return false;
            if (!await CheckpointJoin(page.MaxBy(@event => @event.Context.SequenceNumber)?.Context.SequenceNumber.Next(), version)) return false;
            if (!await _appendedEventsQueues.IsSubscribed(_observerKey)) return false;
        }

        return await CheckpointJoin(tail.Next(), version);
    }

    async Task<bool> SettleJoinPage(IReadOnlyList<AppendedEvent> applicable, int version)
    {
        foreach (var @event in applicable)
        {
            if (!JoinStillCurrent(version) || !await SettleJoinEvent(@event)) return false;
        }
        return true;
    }

    async Task<bool> SettleJoinEvent(AppendedEvent @event)
    {
        var partition = (Key)@event.Context.EventSourceId;
        var number = @event.Context.SequenceNumber;
        if (FailureCovers(partition, number))
        {
            CompleteHeldBackDelivery(partition, number);
            return true;
        }
        if (Failures.TryGet(partition, out var failure))
        {
            if (!await ExtendFailureCoverage(failure, number)) return false;
            CompleteHeldBackDelivery(partition, number);
            return true;
        }
        if (PartitionIsCatchingUp(partition) || State.ReplayingPartitions.Contains(partition))
        {
            RememberCatchUpGap(partition, [@event]);
            return true;
        }

        if (!ShouldHandleEvent(partition, joining: true)) return false;
        _joinRunSettled = false;
        await HandleEventsForPartition(partition, [@event], null, joining: true);
        if (!_joinRunSettled && !FailureCovers(partition, number)) return false;
        CompleteHeldBackDelivery(partition, number);
        return true;
    }

    void HoldGatedLiveRun(Key partition, IEnumerable<AppendedEvent> events)
    {
        if (_catchUpReloadRequired) throw new InvalidOperationException("Catch-up recovery must reload the observer before accepting live delivery.");

        foreach (var @event in events)
        {
            if (!IsOfInterest(_subscription, State.SubscribesToAllEvents, @event)) continue;
            var number = @event.Context.SequenceNumber;
            if (State.NextEventSequenceNumber.IsActualValue && number < State.NextEventSequenceNumber) continue;
            _gatedLiveEvents.Add(number);
            QueueMissedDelivery(partition, number);
        }
    }

    async Task<bool> DrainGatedLiveRuns(EventSequenceNumber tail, int version)
    {
        foreach (var number in _gatedLiveEvents.ToArray())
        {
            if (!JoinStillCurrent(version)) return false;
            if ((!tail.IsActualValue || number > tail) && State.NextEventSequenceNumber <= number &&
                !await WalkJoin(State.NextEventSequenceNumber, number, version)) return false;
            _gatedLiveEvents.Remove(number);
        }

        return JoinStillCurrent(version) && await _appendedEventsQueues.IsSubscribed(_observerKey) && JoinStillCurrent(version);
    }

    void RequireCatchUpReload()
    {
        _catchUpReloadRequired = true;
        _catchUpContinuationVersion++;
        DeactivateOnIdle();
    }

    async Task<bool> CheckpointJoin(EventSequenceNumber? next, int version)
    {
        if (!JoinStillCurrent(version)) return false;
        if (next is null || !next.IsActualValue) return true;
        State = State with { NextEventSequenceNumber = next };
        RewindUncoveredHoles();
        RecordedHandlingState();
        try
        {
            if (await WriteStateAndConfirm()) return JoinStillCurrent(version);
        }
        catch
        {
            RequireCatchUpReload();
            throw;
        }

        RequireCatchUpReload();
        return false;
    }

    void RememberCatchUpGap(Key partition, IEnumerable<AppendedEvent> events)
    {
        foreach (var @event in events)
        {
            if (IsOfInterest(_subscription, State.SubscribesToAllEvents, @event)) QueueMissedDelivery(partition, @event.Context.SequenceNumber);
        }
    }

    void RewindUncoveredHoles()
    {
        if (_shadowClaims.ObserverWide is { Holder: ClaimHolderKind.ObserverCatchUp } wide) RewindCatchUpFloor(wide.ResumePosition);
        foreach (var (partition, first) in UnsettledCatchUpPositions())
        {
            if (!FailureCovers(partition, first)) RewindCatchUpFloor(first);
        }
    }

    IEnumerable<(Key Partition, EventSequenceNumber First)> UnsettledCatchUpPositions() =>
        _missedDeliveries.Select(entry => (entry.Key, entry.Value))
            .Concat(_heldBackDeliveries.Select(entry => (entry.Key, entry.Value.First)))
            .Concat((State.PartitionClaims ?? []).Where(claim => claim.Holder == ClaimHolderKind.PartitionCatchUp &&
                State.CatchingUpPartitions.Contains(claim.Partition)).Select(claim => (claim.Partition, claim.ResumePosition)));

    bool FailureCovers(Key partition, EventSequenceNumber first) => !_alertMutationLock.IsHeld &&
        Failures.TryGet(partition, out var failure) && failure.EffectiveRecoveryCursor is { Mode: FailedPartitionRecoveryMode.Live } cursor &&
        cursor.Position.IsActualValue && cursor.Position <= first;

    void RewindCatchUpFloor(EventSequenceNumber first)
    {
        if (!first.IsActualValue || State.NextEventSequenceNumber.IsActualValue && State.NextEventSequenceNumber <= first) return;
        State = State with { NextEventSequenceNumber = first };
        RecordedHandlingState();
    }
}
