// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_FailedPartitionConverters;

public class when_converting_collection_to_client : Specification
{
    IEnumerable<Contracts.Observation.FailedPartition> _contracts;
    IEnumerable<FailedPartition> _result;

    void Establish() =>
        _contracts =
        [
            new Contracts.Observation.FailedPartition
            {
                Id = Guid.NewGuid(),
                ObserverId = "observer-a",
                Partition = "partition-1",
                Attempts = []
            },
            new Contracts.Observation.FailedPartition
            {
                Id = Guid.NewGuid(),
                ObserverId = "observer-b",
                Partition = "partition-2",
                Attempts = []
            }
        ];

    void Because() => _result = _contracts.ToClient();

    [Fact] void should_return_two_items() => _result.Count().ShouldEqual(2);
    [Fact] void should_convert_first_observer_id() => _result.First().ObserverId.ShouldEqual(new ObserverId("observer-a"));
    [Fact] void should_convert_second_partition() => _result.Skip(1).First().Partition.ShouldEqual(new Partition("partition-2"));
}
