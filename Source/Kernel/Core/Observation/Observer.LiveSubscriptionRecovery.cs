// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer : ILiveSubscriptionRecovery
{
    EventSequenceNumber? _liveSubscriptionRecoveryPosition;
    bool _subscriptionGapAttempted;

    /// <inheritdoc/>
    bool ILiveSubscriptionRecovery.HasSubscriptionGap => _liveSubscriptionRecoveryPosition is not null;

    bool CanRecoverSubscriptionGap => !_isPreparingCatchup && State.CatchingUpPartitions.Count == 0 && !Failures.HasFailedPartitions;

    /// <inheritdoc/>
    void ILiveSubscriptionRecovery.SubscriptionLost(EventSequenceNumber position) => RememberSubscriptionGap(position);

    /// <inheritdoc/>
    async Task ILiveSubscriptionRecovery.RecoverSubscriptionGap()
    {
        if (!CanRecoverSubscriptionGap || _liveSubscriptionRecoveryPosition is not { } position) return;

        // Subscribe precedes both reads. A stale-low tail is not proof that the gap is empty.
        await _eventSequence.GetTailSequenceNumber();
        var next = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(position, _subscription.EventTypes);
        if (_liveSubscriptionRecoveryPosition != position) return;
        if (next.Match(number => !number.IsActualValue, error => error == GetSequenceNumberError.NotFound))
        {
            _liveSubscriptionRecoveryPosition = null;
            return;
        }
        if (next.Match(number => number.IsActualValue, _ => false))
        {
            await RecoverKnownSubscriptionGap();
        }
    }

    async Task RecoverKnownSubscriptionGap()
    {
        if (_subscriptionGapAttempted && IsBackingOffCatchUp) return;

        // Exactly one immediate attempt belongs to this gap, not to each physical rejoin.
        _subscriptionGapAttempted = true;
        _catchUpBackoffUntil = default;
        await CatchUp();
    }

    void RememberSubscriptionGap(EventSequenceNumber position)
    {
        if (!position.IsActualValue) return;
        if (_liveSubscriptionRecoveryPosition is { } previous && previous <= position) return;
        _liveSubscriptionRecoveryPosition = position;
        _subscriptionGapAttempted = false;
    }

    async Task<bool> RestoreSubscriptionDuringBackoff()
    {
        if (State.RunningState == ObserverRunningState.Replaying) return false;
        if (!await _appendedEventsQueues.IsSubscribed(_observerKey))
        {
            RememberSubscriptionGap(State.NextEventSequenceNumber);
            await SettleCatchUpInObserving();
            return true;
        }
        return _liveSubscriptionRecoveryPosition is not null && _subscriptionGapAttempted && IsBackingOffCatchUp;
    }

    async Task SettleCatchUpInObserving()
    {
        if (State.RunningState == ObserverRunningState.Replaying) return;
        if (!await _appendedEventsQueues.IsSubscribed(_observerKey))
        {
            RememberSubscriptionGap(State.NextEventSequenceNumber);
        }
        await TransitionTo<Observing>();
        if (await _appendedEventsQueues.IsSubscribed(_observerKey)) return;

        if (await GetCurrentState() is Observing observing)
        {
            RememberSubscriptionGap(State.NextEventSequenceNumber);
            await observing.RestoreSubscription(State);
        }
    }

    bool CatchUpMadeContiguousProgress(EventSequenceNumber lastHandled, EventSequenceNumber lastScanned) =>
        _liveSubscriptionRecoveryPosition is null && CatchUpMadeProgress(lastHandled, lastScanned);

    void CompleteSubscriptionGap(EventSequenceNumber lastScanned)
    {
        if (!lastScanned.IsActualValue) return;
        if (_liveSubscriptionRecoveryPosition is not { } position) return;
        if (lastScanned < position) return;
        _liveSubscriptionRecoveryPosition = null;
    }

    EventSequenceNumber NextLivePosition(EventSequenceNumber next) =>
        _liveSubscriptionRecoveryPosition is { } position && next > position ? position : next;
}
