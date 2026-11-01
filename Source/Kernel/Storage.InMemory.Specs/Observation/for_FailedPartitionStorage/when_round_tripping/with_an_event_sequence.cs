// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_FailedPartitionStorage.when_round_tripping;

public class with_an_event_sequence : Specification
{
    FailedPartition _result;

    async Task Because()
    {
        using var storage = new FailedPartitionStorage();
        var failure = new FailedPartition { ObserverId = "observer", EventSequenceId = new("A") };
        await storage.Save(failure.ObserverId, new FailedPartitions { Partitions = [failure] });
        _result = (await storage.GetFor(failure.ObserverId)).Partitions.Single();
    }

    [Fact] void should_keep_the_sequence() => _result.EventSequenceId.ShouldEqual(new EventSequenceId("A"));
}
