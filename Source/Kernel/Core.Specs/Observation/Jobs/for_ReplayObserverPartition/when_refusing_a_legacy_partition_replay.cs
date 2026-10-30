// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_refusing_a_legacy_partition_replay : given.a_recovered_global_partition_replay
{
    Exception _error;

    void Establish()
    {
        _request = _request with { HeldBackFrom = EventSequenceNumber.Unavailable };
        _stateStorage.State.Request = _request;
    }

    async Task Because()
    {
        _error = await Catch.Exception(_job.CanResumeForTesting);
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_fail_the_job_terminally() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_touch_the_sink() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_pass_the_missing_bound_without_guessing() => await _observer.Received(1).ReleaseRefusedPartitionReplay(_request.Key, EventSequenceNumber.Unavailable);
}
