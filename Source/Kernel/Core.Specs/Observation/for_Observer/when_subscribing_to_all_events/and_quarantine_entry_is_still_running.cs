// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing_to_all_events;

public class and_quarantine_entry_is_still_running : given.an_observer_explicitly_subscribed_during_quarantine_entry
{
    async Task Because() => await SubscribeDuringQuarantineEntry(allEvents: true, clearQuarantine: false);

    [Fact] void should_defer_recovery_until_cleanup_finishes() => _wasQuarantinedAfterSubscription.ShouldBeTrue();
    [Fact] async Task should_be_observing() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
    [Fact] async Task should_use_the_new_subscription() => (await _observer.GetSubscription()).Arguments.ShouldEqual("new-subscription");
    [Fact] async Task should_subscribe_to_all_events() => (await _observer.GetState()).SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_resume_jobs_with_the_new_subscription() => _recoveryArguments.TrueForAll(arguments => Equals(arguments, "new-subscription")).ShouldBeTrue();
    [Fact] async Task should_leave_quarantine() => (await _observer.IsObserverQuarantined()).ShouldBeFalse();
    [Fact] void should_subscribe_to_the_queue_for_all_event_types_once() => _appendedEventsQueues.Received(1).SubscribeToAllEventTypes(Arg.Any<ObserverKey>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_not_subscribe_to_the_queue_for_specific_event_types() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
    [Fact] void should_resume_the_stopped_catchup_job_once() => _jobsManager.Received(1).Resume(_catchupJobId);
    [Fact] void should_resume_the_stopped_retry_job_once() => _jobsManager.Received(1).Resume(_retryJobId);
    [Fact] void should_not_restart_the_resumed_retry_job() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(request => request.Key == _retryablePartition));
}
