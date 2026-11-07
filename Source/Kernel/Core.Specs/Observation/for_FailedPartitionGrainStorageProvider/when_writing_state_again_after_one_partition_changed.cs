// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state_again_after_one_partition_changed : given.the_provider
{
    GrainId _grainId;
    IGrainState<FailedPartitions> _state;
    List<FailedPartitions> _saved;

    void Establish()
    {
        _grainId = GrainId.Create("type", "key");
        var failedPartitions = new FailedPartitions();
        for (var i = 0; i < 50; i++)
        {
            failedPartitions.RegisterAttempt($"partition-{i}", 1L, ["failed"], string.Empty);
        }

        _state = new GrainState<FailedPartitions> { State = failedPartitions, ETag = "ETag", RecordExists = true };
        _saved = [];
        _failedPartitionsStorage
            .When(_ => _.Save(Arg.Any<Cratis.Chronicle.Concepts.Observation.ObserverId>(), Arg.Any<FailedPartitions>()))
            .Do(call => _saved.Add(Snapshot(call.Arg<FailedPartitions>())));
    }

    async Task Because()
    {
        await provider.WriteStateAsync("name", _grainId, _state);
        await provider.WriteStateAsync("name", _grainId, _state);
        _state.State.RegisterAttempt("partition-7", 2L, ["failed again"], string.Empty);
        await provider.WriteStateAsync("name", _grainId, _state);
    }

    [Fact] void should_save_three_times_at_most() => _saved.Count.ShouldEqual(2);
    [Fact] void should_save_everything_the_first_time() => _saved[0].Partitions.Count().ShouldEqual(50);
    [Fact] void should_save_only_the_changed_partition_the_next_time() => _saved[1].Partitions.Select(_ => _.Partition).ShouldContainOnly((Cratis.Chronicle.Concepts.Keys.Key)"partition-7");

    static FailedPartitions Snapshot(FailedPartitions changes) => new() { Partitions = changes.Partitions.ToArray() };
}
