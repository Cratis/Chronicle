// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_Observer.when_retiring;

public class and_the_projection_observer_is_observing : given.an_observer_with_subscription
{
    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Projection };
        await _observer.Unsubscribe();
        _stateStorage.State = _stateStorage.State with { Identifier = _observerId };
        _observer.SetSubscription(subscription);
        await _observer.TransitionTo<Routing>();
        (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
        _appendedEventsQueues.ClearReceivedCalls();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.Retire();

    [Fact] async Task should_be_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_persist_the_disconnected_running_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Disconnected);
    [Fact] async Task should_unsubscribe_from_the_appended_events_queue() => await _appendedEventsQueues.Received(1).Unsubscribe(new AppendedEventsQueueSubscription(_observerKey, 0));
    [Fact] async Task should_no_longer_have_a_subscriber() => (await _observer.IsSubscribed()).ShouldBeFalse();
    [Fact] async Task should_clear_incidents_as_removed() => await _observerAlerts.Received(1).Removed();
    [Fact] async Task should_not_report_a_recovery() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Any<ObserverAlertSnapshot>());
    [Fact]
    async Task should_pass_the_removal_guard()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var observer = Substitute.For<IObserver>();
        observer.IsSubscribed().Returns(await _observer.IsSubscribed());
        observer.GetState().Returns(await _observer.GetState());
        grainFactory.GetGrain<IObserver>(_observerKey).Returns(observer);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        grainFactory.GetGrain<INamespaces>(_observerKey.EventStore).Returns(namespaces);
        _eventStoreStorage.Observers.Has(_observerId).Returns(true);
        grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_observerKey.EventStore, _observerKey.Namespace)).Returns(_jobsManager);
        var remover = new ObserverRemover(grainFactory, _storage, NullLogger<ObserverRemover>.Instance);

        (await remover.Remove(_observerKey.EventStore, _observerId, _observerKey.EventSequenceId)).Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    }
}
