// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_another_instance_of_the_client_is_observing_with_the_same_definition : given.an_observer_restoring_another_client_instance
{
    ObserverSubscription _subscription;

    async Task Because()
    {
        await RestoreSecondClient();
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_have_both_client_instances_as_targets() => _subscription.Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_firstClient.ConnectionId, _secondClient.ConnectionId);
    [Fact] void should_not_write_observer_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_the_definition() => _definitionStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_write_failed_partitions() => _failedPartitionsStorageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_look_up_unfinished_jobs() => _jobsManager.DidNotReceive().GetUnfinishedJobs();
    [Fact] void should_refresh_the_event_type_schemas_once() => _eventTypesStorage.Received(1).GetFor(Arg.Any<IEnumerable<EventType>>());
    [Fact] async Task should_still_be_observing() => (await _observer.GetState()).RunningState.ShouldEqual(ObserverRunningState.Active);
    [Fact] async Task should_know_the_second_client() => (await _observer.KnowsClient(_secondClient.ConnectionId)).ShouldBeTrue();
}
