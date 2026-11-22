// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    TimeSpan _dormantAfter;
    DateTimeOffset? _dormantSince;
    bool _releasedWhenDormant;
    bool _safetyNetWake;

    /// <summary>
    /// Gets whether the activation was created from a durable subscription and has handled nothing since.
    /// </summary>
    /// <remarks>
    /// An activation woken only for its safety net has nothing to do after the watchdog pass, so it is released at once instead
    /// of staying resident for a whole dormancy period; one woken by an event stays for the period like any other.
    /// </remarks>
    bool _wokenFromDormancyAndUnused;

    /// <summary>
    /// Gets whether dormant release and wake apply to this observer right now.
    /// </summary>
    /// <remarks>
    /// Evaluated at every decision: a rolling upgrade can add an older silo, which does not understand dormancy, to a cluster
    /// that had none (decision 0034 point 5).
    /// </remarks>
    bool DormantReleaseAllowed =>
        _dormantAfter > TimeSpan.Zero &&
        _durableSubscriptionMode == DurableSubscriptionMode.Release &&
        DormantObserverFloor.IsReached(clusterReleases);

    /// <summary>
    /// Gets whether the observer's record of its subscription is the one in its state, written to storage.
    /// </summary>
    bool IsDurableSubscriptionPersisted =>
        _persistedStateWrites > 0 &&
        !_forgottenDurableSubscriptionIsPending &&
        State.DurableSubscription is { } persisted &&
        ReferenceEquals(persisted, DurableSubscriptionToWrite());

    /// <summary>
    /// Gets whether the observer is dormant but for the record of its subscription not having been written yet.
    /// </summary>
    /// <remarks>
    /// Every condition of <see cref="IsIdle"/> except that it has no subscription (see <see cref="NothingIsOwed"/>), and the
    /// subscription must be of a kind that is released and held by something that can be woken: a client reactor, client
    /// reducer or projection, with at least one client when a client serves it. On top of that the observer must be observing
    /// with nothing delivered, held, claimed, fenced, failing, replaying or catching up, and no lane of the queue holding events
    /// for it, which the queue checks when the observer asks it to keep the subscription (see <see cref="IsReleasedAsDormant"/>).
    /// Every one of these keeps the observer resident while it holds. The standbys of the subscription do not: the record names
    /// them with the targets (see <see cref="IsDurableSubscriptionPersisted"/>), and the observer has them again when it wakes.
    /// </remarks>
    bool IsDormantApartFromTheRecord =>
        !_removed &&
        _observerExists &&
        !IsRetired &&
        _subscription.IsSubscribed &&
        !_subscriptionSetupFailed &&
        InScopeForDormancy &&
        State.RunningState == ObserverRunningState.Active &&
        State.NextEventSequenceNumber.IsActualValue &&
        NothingIsOwed &&
        State.InFlightPartitions.Count == 0 &&
        _liveDeliveriesInFlight == 0 &&
        _catchUpHandoversInFlight == 0 &&
        _catchUpBackoffUntil == default &&
        _shortCatchUpRetries == 0 &&
        !_isPreparingCatchup &&
        !_walkingEveryPartition &&
        !_recoverSubscriptionAfterQuarantine &&
        !_retryRecoveryAfterQuarantine &&
        _pageFloor is null &&
        _prefetchedPartitions.Count == 0 &&
        !_deliveryFences.Active(_clock.GetUtcNow()).Any() &&
        HasNoClaims;

    bool IsDormant => IsDormantApartFromTheRecord && IsDurableSubscriptionPersisted;

    bool IsReleasedAsDormant => _releasedWhenDormant && IsDormant && DormantReleaseAllowed;

    /// <summary>
    /// Gets whether the subscription is of a kind that is released when dormant and can be woken from its record.
    /// </summary>
    bool InScopeForDormancy =>
        _subscription.SubscriberType.GetSubscriptionKind() is { } kind &&
        kind.CanBeReleasedWhenDormant() &&
        _subscription.GenerationDelivery == EventGenerationDelivery.Compatibility &&
        (!kind.IsServedByClient() || _subscription.Targets.Count > 0);

    bool HasNoClaims
    {
        get
        {
            var claims = ShadowClaimsSnapshot();
            return claims.Wide is null && !(claims.Claims?.Any() ?? false);
        }
    }

    /// <inheritdoc/>
    public async Task RunWatchdogOnce()
    {
        _safetyNetWake = true;
        try
        {
            await Watchdog(CancellationToken.None);
        }
        finally
        {
            _safetyNetWake = false;
        }
    }

    void ConfigureDormancy(Observers config) =>
        _dormantAfter = TimeSpan.FromSeconds(Math.Max(config.DormantAfter, 0));

    /// <summary>
    /// Releases the observer when it has been dormant for the dormancy period.
    /// </summary>
    /// <returns>True when the observer asked to be deactivated; otherwise false.</returns>
    /// <remarks>
    /// Run at the end of a watchdog pass, once everything the pass checks has been checked: a client that left has been found,
    /// a stalled job nudged. A tick that finds the observer not dormant starts the period over.
    /// </remarks>
    async Task<bool> ReleaseWhenDormant()
    {
        if (_releasedWhenDormant)
        {
            // Orleans deactivates the activation as soon as it asked, so a tick should not get here. Should one, the
            // activation either asks again or, if work arrived in the meantime, carries on as before.
            if (IsDormant)
            {
                DeactivateOnIdle();
                return true;
            }

            _releasedWhenDormant = false;
            _dormantSince = null;
            return false;
        }

        if (!DormantReleaseAllowed || !IsDormantApartFromTheRecord)
        {
            _dormantSince = null;
            return false;
        }

        // The record is written with the state; a subscription changed since the last write (a client left, a target moved)
        // is written once here so the record that outlives the activation is the one the activation holds.
        if (!IsDurableSubscriptionPersisted)
        {
            RecordDurableSubscription();
            await WriteStateAsync();
            if (!IsDormant)
            {
                _dormantSince = null;
                return false;
            }
        }

        var now = _clock.GetUtcNow();
        _dormantSince ??= now;
        var releaseAtOnce = _safetyNetWake && _wokenFromDormancyAndUnused;
        if (!releaseAtOnce && now - _dormantSince.Value < _dormantAfter)
        {
            return false;
        }

        logger.ReleasingDormantObserver(_dormantAfter);
        _releasedWhenDormant = true;
        DeactivateOnIdle();
        return true;
    }

    /// <summary>
    /// Asks the appended events queue to keep the subscription of the observer that is leaving, so an event wakes it.
    /// </summary>
    /// <returns>True when the queue keeps it; false when the observer must leave the ordinary way.</returns>
    async Task<bool> KeepQueueEntryForDormantRelease()
    {
        try
        {
            return await _appendedEventsQueues.TryMarkDormant(_observerKey);
        }
        catch (Exception ex) when (ObservationOnlyFailures.CanBeContained(ex))
        {
            logger.KeepingDormantEntryFailed(ex);
            return false;
        }
    }

    void NoteHandledAfterWake() => _wokenFromDormancyAndUnused = false;
}
