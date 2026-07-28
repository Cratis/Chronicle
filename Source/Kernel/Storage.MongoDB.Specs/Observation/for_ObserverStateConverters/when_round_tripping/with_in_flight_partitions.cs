// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverStateConverters.when_round_tripping;

public class with_in_flight_partitions : Specification
{
    static readonly Key _firstPartition = "partition-one";
    static readonly Key _secondPartition = "partition-two";

    ObserverState _mongo;
    KernelObserverState _roundTripped;

    void Because()
    {
        _mongo = new KernelObserverState
        {
            Identifier = "some-observer",
            InFlightPartitions = new HashSet<Key> { _firstPartition, _secondPartition }
        }.ToMongoDB();
        _roundTripped = _mongo.ToKernel();
    }

    [Fact] void should_persist_the_in_flight_partitions_in_the_mongodb_representation() =>
        _mongo.InFlightPartitions.ShouldContainOnly([_firstPartition, _secondPartition]);

    [Fact] void should_restore_the_in_flight_partitions_on_the_kernel_representation() =>
        _roundTripped.InFlightPartitions.ShouldContainOnly([_firstPartition, _secondPartition]);
}
