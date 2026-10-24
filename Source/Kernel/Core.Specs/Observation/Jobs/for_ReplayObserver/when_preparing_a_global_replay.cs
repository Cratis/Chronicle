// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_preparing_a_global_replay : given.a_recovered_global_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(() => _job.PrepareStepsForTesting(_request));
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_not_mark_replay_as_started() => _stateStorage.State.ReplayStartedAt.ShouldEqual(DateTimeOffset.MinValue);
    [Fact] async Task should_not_read_failed_partitions() => await _observer.DidNotReceive().GetFailedPartitionKeys();
    [Fact] async Task should_not_begin_replay_on_silos() => await _replayServiceClient.DidNotReceive().BeginReplayFor(Arg.Any<ObserverDetails>());
    [Fact] async Task should_not_visit_the_key_index() => await _keyIndexes.DidNotReceive().GetFor(Arg.Any<ObserverKey>(), Arg.Any<ObserverReaderScope>());
    [Fact] void should_not_write_job_steps() => _jobStepStorage.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedReplay();
    [Fact] void should_not_touch_the_sink() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
}
