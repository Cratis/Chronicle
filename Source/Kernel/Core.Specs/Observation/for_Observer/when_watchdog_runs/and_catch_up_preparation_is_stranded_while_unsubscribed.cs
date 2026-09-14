// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// The stranded-catch-up-preparation rescue used to only re-route the observer when it was still subscribed,
/// leaving the preparing-catch-up flag cleared but the state machine parked wherever it happened to be otherwise -
/// a second wedge layered on top of the one the rescue exists to fix. Re-routing must happen unconditionally so the
/// observer always settles into a state consistent with its subscription, subscribed or not.
/// </summary>
public class and_catch_up_preparation_is_stranded_while_unsubscribed : given.an_observer_with_client_owned_subscription
{
    bool _isPreparingCatchupAfterWatchdog;

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _appendedEventsQueues.ClearReceivedCalls();
        _jobsManager.ClearReceivedCalls();
    }

    async Task Because()
    {
        // CatchUp fails to get a job (Start<ICatchUpObserver,...> is unconfigured), leaving the observer preparing
        // catch-up. The subscription is then cleared directly - the same desync a client disconnecting mid-flight
        // would leave behind - without moving the state machine off Observing, so the watchdog's rescue is the only
        // thing that can still carry it forward.
        await _observer.CatchUp();
        _observer.SetSubscription(ObserverSubscription.Unsubscribed);
        await _observer.RunWatchdogAsync();
        _isPreparingCatchupAfterWatchdog = await _observer.IsPreparingCatchup();
    }

    [Fact] void should_clear_the_stranded_catch_up_preparation() => _isPreparingCatchupAfterWatchdog.ShouldBeFalse();

    [Fact] void should_still_route_the_observer_through_to_disconnected() =>
        _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
}
