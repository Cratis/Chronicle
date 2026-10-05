// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reconciling_pattern_capture;

public class and_capture_is_already_subscribed : given.an_event_sequence_with_a_capture_observer
{
    Task Establish() => SubscribeCapture();

    Task Because() => _silo.TimerRegistry.FireAllAsync();

    [Fact] async Task should_let_the_observer_check_its_actual_state() => await _patternCapture.Received(1).RecoverSubscription(EventStore, EventStoreNamespace);
    [Fact] async Task should_keep_observing() => (await _captureObserver.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_not_repeat_setup() => await _jobsManager.Received(1).GetJobs(Arg.Any<JobQuery>());
    [Fact] async Task should_not_resubscribe_to_the_queue() => await _appendedEventsQueues.Received(1).Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
