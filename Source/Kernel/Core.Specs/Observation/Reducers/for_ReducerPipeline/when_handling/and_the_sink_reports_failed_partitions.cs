// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_handling;

/// <summary>
/// A bulk flush can drop other partitions' writes along with this one's, so every failed partition is carried to the
/// caller instead of only the first.
/// </summary>
public class and_the_sink_reports_failed_partitions : given.a_pipeline_with_a_recording_sink
{
    readonly FailedPartition _own = new(EventSourceIdValue, 1UL) { Reason = "own" };
    readonly FailedPartition _other = new("another-event-source", 2UL) { Reason = "other" };
    Exception? _error;

    void Establish() => _sink.FailedPartitions = [_own, _other];

    async Task Because() => _error = await Catch.Exception(() => _pipeline.Reduce(CreateContext(), CreateReducer(NewState())));

    [Fact] void should_throw_a_sink_write_failure() => _error.ShouldBeOfExactType<ReducerSinkWriteFailed>();
    [Fact] void should_carry_every_failed_partition() => ((ReducerSinkWriteFailed)_error!).FailedPartitions.ShouldContainOnly([_own, _other]);
}
