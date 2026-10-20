// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserver;

public class when_completing_a_recovered_global_replay : given.a_recovered_global_replay
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(_job.CompleteForTesting);

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_fail_the_job() => _stateStorage.State.Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_promote_or_abandon_a_sink() => _replayServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_notify_or_resolve_failed_partitions() => _observer.ReceivedCalls().ShouldBeEmpty();
}
