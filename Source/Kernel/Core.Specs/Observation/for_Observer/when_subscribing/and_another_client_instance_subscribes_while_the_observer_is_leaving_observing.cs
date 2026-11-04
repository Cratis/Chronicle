// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

/// <summary>
/// An interleaving call can start moving the observer out of Observing while a handover waits on its queue binding.
/// Leaving Observing awaits the queue unsubscribe with the current state still Observing and the running state still
/// active, so only knowing that a transition is in progress keeps the handover from adding a target to an observer
/// that is on its way out. The full subscribe runs instead; leaving finishes before it reaches its own transition.
/// </summary>
public class and_another_client_instance_subscribes_while_the_observer_is_leaving_observing : given.an_observer_served_by_one_client_instance
{
    readonly TaskCompletionSource<bool> _bindingQuery = new();
    readonly TaskCompletionSource _unsubscribing = new();
    bool _leavingObserving;
    bool _fullSubscribeRan;

    void Establish()
    {
        _appendedEventsQueues.IsSubscribed(Arg.Any<ObserverKey>()).Returns(_bindingQuery.Task, Task.FromResult(true));
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _leavingObserving = true;
            return _unsubscribing.Task;
        });

        // Only the full subscribe reloads the running state history; the handover never reads it.
        _eventStoreNamespaceStorage.Observers.GetRunningStateChangedAt(Arg.Any<ObserverId>()).Returns(_ =>
        {
            _fullSubscribeRan = true;
            _unsubscribing.TrySetResult();
            return _persistedRunningStateChangedAt;
        });
    }

    async Task Because()
    {
        var subscribing = SubscribeSecondClient();
        var leaving = _observer.TransitionTo<Routing>();
        _leavingObserving.ShouldBeTrue();
        _bindingQuery.SetResult(true);
        await subscribing;
        _unsubscribing.TrySetResult();
        await leaving;
    }

    [Fact] void should_run_the_full_subscribe() => _fullSubscribeRan.ShouldBeTrue();
    [Fact] async Task should_have_both_client_instances_as_targets() => (await _observer.GetSubscription()).Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_firstClient.ConnectionId, _secondClient.ConnectionId);
}
