// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A queue that spills to catch-up under back-pressure drops its subscriptions and relies on the catch-up trigger to
/// recover each observer. When that trigger never succeeds, the observer is left active and behind with no catch-up
/// job and no live delivery, and nothing reactivates it since it is kept alive. The watchdog must rescue it.
/// </summary>
public class and_the_queue_no_longer_holds_the_subscription : given.an_observer_behind_on_a_relevant_event
{
    void Establish() => _appendedEventsQueues.IsSubscribed(_observerKey).Returns(false);

    async Task Because() => await _observer.RunWatchdogAsync();

    /// <summary>
    /// The key is matched loosely on purpose. Observing.OnEnter builds the subscribe key from
    /// ObserverState.Identifier, which the storage provider stamps in production but which stays
    /// ObserverId.Unspecified under the test kit, so asserting the exact key here would fail for a
    /// reason that cannot occur at runtime.
    /// </summary>
    [Fact] void should_resubscribe_the_observer_to_its_queue() => _appendedEventsQueues
        .Received(1)
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());

    [Fact] void should_start_catching_up_from_where_the_observer_left_off() => _jobsManager
        .Received(1)
        .Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(request =>
            request.ObserverKey == _observerKey &&
            request.FromEventSequenceNumber == _nextSequenceNumber));

    [Fact] void should_leave_the_observer_active() =>
        _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
