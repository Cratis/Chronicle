// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

public class and_the_final_flush_fails : given.a_projection_replay_handler_with_projection
{
    readonly Key _partition = "partition";
    readonly EventSequenceNumber _sequenceNumber = 42UL;
    ReplayContext _replayContext;
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish()
    {
        _replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(_replayContext);
        _projectionPipeline.EndReplay(_replayContext).Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([new(_partition, _sequenceNumber)]));
    }

    async Task Because() => _result = await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_record_the_failed_partition() => _observer.Received(1).PartitionsFailed(Arg.Is<IReadOnlyCollection<SinkFailedPartition>>(failures => failures.Single().EventSourceId == _partition && failures.Single().EventSequenceNumber == _sequenceNumber));
    [Fact] void should_not_signal_replayed_to_replay_manager() => _readModelReplayManager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
    [Fact] void should_not_evict_the_replay_context() => _replayContexts.DidNotReceiveWithAnyArgs().Evict(default!);
    [Fact] void should_report_a_finalization_error() => _result.TryGetError(out _).ShouldBeTrue();
}
