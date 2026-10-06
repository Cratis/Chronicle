// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

public class and_the_final_flush_throws_with_failed_partitions : given.a_projection_replay_handler_with_projection
{
    readonly Key _partition = "partition";
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish()
    {
        var context = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UnixEpoch);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(context);
        _projectionPipeline.EndReplay(context).Returns(Task.FromException<IEnumerable<SinkFailedPartition>>(new BulkWriteFailed([new(_partition, 42UL)], new BulkServerUnavailable())));
    }

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_record_the_failures_carried_by_the_exception() => _observer.Received(1).PartitionFailed(_partition, 42UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_not_signal_a_successful_replay() => _readModelReplayManager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
    [Fact] void should_not_evict_the_replay_context() => _replayContexts.DidNotReceiveWithAnyArgs().Evict(default!);
    [Fact] void should_report_a_finalization_error() => _result.TryGetError(out _).ShouldBeTrue();

    sealed class BulkServerUnavailable() : Exception("The server did not answer");
}
