// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_FailedPartitionGrainStorageProvider;

public class when_writing_state_that_was_read_from_storage : given.the_provider
{
    GrainId _grainId;
    IGrainState<FailedPartitions> _state;

    void Establish()
    {
        _grainId = GrainId.Create("type", "key");
        _state = new GrainState<FailedPartitions>();
        _failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(new FailedPartitions { Partitions = [new() { Partition = "stored", ObserverId = ObserverKey.Parse(_grainId.Key.ToString()).ObserverId }] }));
    }

    async Task Because()
    {
        await provider.ReadStateAsync("name", _grainId, _state);
        await provider.WriteStateAsync("name", _grainId, _state);
    }

    [Fact] void should_not_write_what_storage_already_holds() => _ = _failedPartitionsStorage.DidNotReceive().Save(Arg.Any<ObserverId>(), Arg.Any<FailedPartitions>());
}
