// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.ReadModels;

using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.for_ProjectionReplayHandler.when_ending_replay;

/// <summary>
/// The reason the sink gives for a failed partition is what makes the failed partition record actionable, so it is
/// recorded with it rather than a bare "Bulk operation failed".
/// </summary>
public class and_the_final_flush_fails_with_a_reason : given.a_projection_replay_handler_with_projection
{
    const string Reason = "MongoDB write error 11000 (DuplicateKey): E11000 duplicate key error";
    readonly Key _partition = "partition";
    readonly EventSequenceNumber _sequenceNumber = 42UL;
    IEnumerable<string> _messages = [];

    void Establish()
    {
        var replayContext = new ReplayContext(_readModelType, _readModelName, "TheRevertModel", DateTimeOffset.UtcNow);
        _replayContexts.TryGet(_readModelType.Identifier).Returns(replayContext);
        _projectionPipeline.EndReplay(replayContext).Returns(Task.FromResult<IEnumerable<SinkFailedPartition>>([new(_partition, _sequenceNumber) { Reason = Reason }]));
        _observer
            .When(_ => _.PartitionFailed(_partition, _sequenceNumber, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Handling))
            .Do(call => _messages = call.ArgAt<IEnumerable<string>>(2).ToArray());
    }

    async Task Because() => await _handler.EndReplayFor(_observerDetails);

    [Fact] void should_record_the_partition() => _messages.Single().ShouldContain(_partition.ToString());
    [Fact] void should_record_the_reason() => _messages.Single().ShouldContain(Reason);
}
