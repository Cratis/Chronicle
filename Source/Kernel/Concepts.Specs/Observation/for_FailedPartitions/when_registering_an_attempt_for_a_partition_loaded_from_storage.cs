// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_registering_an_attempt_for_a_partition_loaded_from_storage : Specification
{
    static readonly Key _partition = new("some-partition", ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _loaded;
    FailedPartition _result;

    void Establish()
    {
        // A partition read back from storage carries the observer identity stamped at write time,
        // while one created in-memory does not - registering against the loaded entry must find it
        // rather than create a second entry for the same partition.
        _loaded = new() { Partition = _partition, ObserverId = "the-observer" };
        _failedPartitions = new() { Partitions = [_loaded] };
    }

    void Because() => _result = _failedPartitions.RegisterAttempt(_partition, EventSequenceNumber.First, ["it failed again"], string.Empty);

    [Fact] void should_register_against_the_loaded_entry() => _result.ShouldEqual(_loaded);
    [Fact] void should_keep_a_single_entry() => _failedPartitions.Partitions.Count().ShouldEqual(1);
    [Fact] void should_add_the_attempt() => _loaded.Attempts.Count().ShouldEqual(1);
}
