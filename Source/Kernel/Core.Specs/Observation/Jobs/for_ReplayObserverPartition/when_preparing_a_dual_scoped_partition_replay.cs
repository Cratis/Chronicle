// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;

public class when_preparing_a_dual_scoped_partition_replay : given.a_recovered_global_partition_replay
{
    Exception _error;

    async Task Because()
    {
        _error = await Catch.Exception(() => _job.PrepareStepsForTesting(_request));
        await _job.CompletionNotification;
    }

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<GlobalProjectionReplayNotSupported>();
    [Fact] void should_not_dispatch_to_silos() => _replayStateServiceClient.ReceivedCalls().ShouldBeEmpty();
    [Fact] async Task should_release_without_claiming_progress() => await _observer.Received(1).ReleaseRefusedPartitionReplay(_request.Key, _request.HeldBackFrom);
}
