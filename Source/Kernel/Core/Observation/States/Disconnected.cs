// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.States;

/// <summary>
/// Represents the disconnected state of an observer.
/// </summary>
public class Disconnected : BaseObserverState
{
    bool _quarantiningUnrecoverableSubscription;
    bool _resumingRetainedReplay;

    /// <inheritdoc/>
    public override ObserverRunningState RunningState => ObserverRunningState.Disconnected;

    /// <inheritdoc/>
    /// <remarks>
    /// <see cref="QuarantinedObserver"/> is only allowed while <see cref="QuarantineUnrecoverableSubscription"/> is
    /// giving up on recovering a subscription. Anything else that asks a disconnected observer to quarantine is refused,
    /// as it always has been.
    /// </remarks>
    protected override IImmutableList<Type> AllowedTransitions => (_resumingRetainedReplay, _quarantiningUnrecoverableSubscription) switch
    {
        (true, _) => [typeof(Routing), typeof(CatchingUpInFlight), typeof(Replay)],
        (_, true) => [typeof(Routing), typeof(CatchingUpInFlight), typeof(QuarantinedObserver)],
        _ => [typeof(Routing), typeof(CatchingUpInFlight)]
    };

    /// <inheritdoc/>
    public override Task<ObserverState> OnEnter(ObserverState state) => Task.FromResult(state);

    /// <inheritdoc/>
    public override Task<ObserverState> OnLeave(ObserverState state) => Task.FromResult(state);

    /// <summary>
    /// Resumes replay retained by a terminal webhook retirement after explicit registration.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task ResumeRetainedReplay()
    {
        _resumingRetainedReplay = true;
        try
        {
            await StateMachine.TransitionTo<Replay>();
        }
        finally
        {
            _resumingRetainedReplay = false;
        }
    }

    /// <summary>
    /// Quarantines a subscribed observer whose recovery after an earlier quarantine keeps failing, so that an operator
    /// sees it and can clear it again, rather than leaving it disconnected with nothing to show it needs attention.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal async Task QuarantineUnrecoverableSubscription()
    {
        _quarantiningUnrecoverableSubscription = true;
        try
        {
            await StateMachine.TransitionTo<QuarantinedObserver>();
        }
        finally
        {
            _quarantiningUnrecoverableSubscription = false;
        }
    }
}
