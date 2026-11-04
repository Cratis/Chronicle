// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_receiving_replay_notifications;

public class and_partition_callbacks_bracket_their_events : given.a_notifiable_reactor_stream
{
    const string PartitionId = "order-42/replay";
    bool _beginWasIncomplete;
    bool _endWasIncomplete;
    ReactorResult[] _results;

    async Task Because()
    {
        Notify(ReplayState.BeginReplayPartition, PartitionId);
        Deliver(PartitionId, "replay", 201);
        Notify(ReplayState.EndReplayPartition, PartitionId);
        Deliver(PartitionId, "sentinel", 202);

        await _work.Entered($"begin:{PartitionId}");
        _beginWasIncomplete = !_work.IsCompleted($"begin:{PartitionId}");
        _work.Release($"begin:{PartitionId}");
        await _work.Entered("replay");
        _work.Release("replay");
        await _work.Entered($"end:{PartitionId}");
        _endWasIncomplete = !_work.IsCompleted($"end:{PartitionId}");
        _work.Release($"end:{PartitionId}");
        await _work.Entered("sentinel");
        _work.Release("sentinel");
        _results = [await Answered(201), await Answered(202)];
    }

    [Fact] void should_await_an_asynchronous_partition_begin_callback() => _beginWasIncomplete.ShouldBeTrue();
    [Fact] void should_await_an_asynchronous_partition_end_callback() => _endWasIncomplete.ShouldBeTrue();
    [Fact] void should_bracket_only_the_named_partition_with_its_callbacks() => _work.Trace.ShouldEqual<string>([
        $"begin:{PartitionId}:entered", $"begin:{PartitionId}:completed",
        "replay:entered", "replay:completed",
        $"end:{PartitionId}:entered", $"end:{PartitionId}:completed",
        "sentinel:entered", "sentinel:completed"
    ]);
    [Fact] void should_successfully_handle_every_event() => _results.Select(result => result.State).ShouldEqual([ObservationState.Success, ObservationState.Success]);
}
