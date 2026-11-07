// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state_after_a_partition_resolved : given.the_provider
{
    GrainId _grainId;
    IGrainState<FailedPartitions> _state;
    int _removalsSaved;

    void Establish()
    {
        _grainId = GrainId.Create("type", "key");
        var failedPartitions = new FailedPartitions();
        failedPartitions.RegisterAttempt("resolved", 1L, ["failed"], string.Empty);
        failedPartitions.RegisterAttempt("kept", 1L, ["failed"], string.Empty);
        _state = new GrainState<FailedPartitions> { State = failedPartitions, ETag = "ETag", RecordExists = true };
        _failedPartitionsStorage
            .When(_ => _.Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>()))
            .Do(call => _removalsSaved += call.Arg<FailedPartitions>().ResolvedPartitions.Count());
    }

    async Task Because()
    {
        await provider.WriteStateAsync("name", _grainId, _state);
        _state.State.Remove("resolved");
        await provider.WriteStateAsync("name", _grainId, _state);
        await provider.WriteStateAsync("name", _grainId, _state);
    }

    [Fact] void should_remove_it_from_storage_once() => _removalsSaved.ShouldEqual(1);
}
