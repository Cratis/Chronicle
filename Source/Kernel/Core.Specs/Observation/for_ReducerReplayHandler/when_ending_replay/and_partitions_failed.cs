// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_ending_replay;

public class and_partitions_failed : given.a_reducer_replay_handler
{
    void Establish() => _pipeline.EndReplay(_context).Returns(Task.FromResult<IEnumerable<FailedPartition>>([new("partition", 42UL)]));
    async Task Because() => _result = await _handler.EndReplayFor(_details);

    [Fact] void should_record_failed_partition() => _observer.Received(1).PartitionFailed("partition", 42UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Handling);
    [Fact] void should_not_record_success() => _manager.DidNotReceiveWithAnyArgs().Replayed(default!, default!);
    [Fact] void should_not_evict_context() => _contexts.DidNotReceiveWithAnyArgs().Evict(default!);
    [Fact] void should_report_error() => _result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_notify_clients_of_end() => _mediator.Received(1).OnEndReplay(new("reducer"), _details.Key.EventStore, _details.Key.Namespace);
}
