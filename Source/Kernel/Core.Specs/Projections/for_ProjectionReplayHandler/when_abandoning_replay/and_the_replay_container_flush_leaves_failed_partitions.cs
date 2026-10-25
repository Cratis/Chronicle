// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_abandoning_replay;

public class and_the_replay_container_flush_leaves_failed_partitions : given.a_projection_replay_handler_with_projection
{
    readonly Key _partition = "partition";
    readonly EventSequenceNumber _sequenceNumber = 42UL;
    ReplayContext _replayContext;
    Result<ICanHandleReplayForObserver.Error> _result;

    void Establish()
    {
        _replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(_replayContext);
        _projectionPipeline.EndBulk().Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([new(_partition, _sequenceNumber)]));
    }

    async Task Because() => _result = await _handler.AbandonReplayFor(_observerDetails);

    [Fact] void should_not_record_the_failed_partition_of_the_discarded_container() => _observer.DidNotReceive().PartitionFailed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
    [Fact] void should_leave_replay() => _projectionPipeline.Received(1).LeaveReplay();
    [Fact] void should_evict_the_replay_context() => _replayContexts.Received(1).Evict(_readModelType.Identifier);
    [Fact] void should_not_report_an_error() => _result.TryGetError(out _).ShouldBeFalse();
}
