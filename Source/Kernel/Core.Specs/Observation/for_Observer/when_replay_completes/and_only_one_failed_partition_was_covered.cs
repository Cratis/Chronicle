// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_replay_completes;

public class and_only_one_failed_partition_was_covered : given.an_observer
{
    static readonly Key _filteredPartition = "filtered-partition";
    static readonly Key _recoveredPartition = "recovered-partition";
    static readonly EventType _excludedType = new("e9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    static readonly EventType _includedType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { FailedPartitionCount = 2 };
        _failedPartitionsStorage.State.AddFailedPartition(_filteredPartition, 12UL);
        _failedPartitionsStorage.State.AddFailedPartition(_recoveredPartition, 17UL);
        GivenFailedEventAt(_filteredPartition, 12UL, _excludedType);
        GivenFailedEventAt(_recoveredPartition, 17UL, _includedType);
    }

    async Task Because() => await _observer.ReplayedSuccessfully(
        42UL,
        new Dictionary<Key, EventSequenceNumber> { [_filteredPartition] = 19UL, [_recoveredPartition] = 42UL },
        [_includedType]);

    [Fact] void should_keep_the_filtered_failure() => _failedPartitionsStorage.State.Partitions.Single().Partition.ShouldEqual(_filteredPartition);
    [Fact] void should_resolve_the_covered_failure() => _failedPartitionsStorage.State.ResolvedPartitions.Single().Partition.ShouldEqual(_recoveredPartition);
    [Fact] void should_count_only_the_remaining_failure() => _stateStorage.State.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
