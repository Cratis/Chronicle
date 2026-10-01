// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the quarantined state of an observer.
/// </summary>
/// <param name="observerKey">The <see cref="ObserverKey"/> for the observer.</param>
/// <param name="logger">Logger for logging.</param>
public class QuarantinedObserver(
    ObserverKey observerKey,
    ILogger<QuarantinedObserver> logger) : BaseObserverState
{
    bool _leavingForSubscription;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Quarantined;

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="Disconnected"/> is only allowed while <see cref="LeaveForSubscription"/> is leaving the quarantine.
    /// Deactivation and unsubscribing also ask for <see cref="Disconnected"/>, and refusing them is what keeps
    /// an observer quarantined until it is subscribed again or the quarantine is cleared.
    /// </remarks>
    protected override IImmutableList<Type> AllowedTransitions => _leavingForSubscription
        ? [typeof(Routing), typeof(Disconnected)]
        : [typeof(Routing)];

    /// <inheritdoc/>
    public override async Task<ObserverState> OnEnter(ObserverState state)
    {
        using var scope = logger.BeginScope(new
        {
            state.Identifier,
            observerKey.EventStore,
            observerKey.Namespace,
            observerKey.EventSequenceId
        });
        logger.ObserverQuarantined();

        var observer = (Observer)Observer;

        // OnEnter also runs when a quarantined observer is activated again, which resumes the quarantine rather than
        // starting one. The observer knows which of the two this is and only counts a quarantine that starts.
        observer.RecordObserverQuarantined();
        await observer.RemoveFailedPartitionReminders();
        await observer.StopAllRetryFailedPartitionJobs();
        await observer.ReportAlertState();

        return state;
    }

    /// <summary>
    /// Leaves the quarantine for a subscription by moving to <see cref="Disconnected"/>, the state an observer is in
    /// when its client connects. Unlike <see cref="Routing"/>, entering it changes nothing but the running state, so
    /// replay, the partitions catching up and the failed partitions are left as they are for the subscription to
    /// handle the way it does for any other observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task LeaveForSubscription()
    {
        _leavingForSubscription = true;
        try
        {
            await StateMachine.TransitionTo<Disconnected>();
        }
        finally
        {
            _leavingForSubscription = false;
        }
    }
}
