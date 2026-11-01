// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_newer_failure_exists_on_reactivation : given.a_partition_replay_covering_a_failure
{
    FailedPartitionId _newEpisode;

    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        _failedPartitionsStorage.State = new();
        _failedPartitionsStorage.State.RegisterAttempt(_partition, 45UL, ["New failure"], string.Empty);
        _newEpisode = _failedPartitionsStorage.State.Partitions.Single().Id;
        await _failedPartitionsStorage.WriteStateAsync();
        await Crash();
        _jobsManager.ClearReceivedCalls();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_retain_the_new_episode() => _failureStorage.PersistedState.Partitions.Single().Id.ShouldEqual(_newEpisode);
    [Fact] void should_keep_the_new_episode_unresolved() => _failureStorage.PersistedState.HasFailedPartitions.ShouldBeTrue();
}
