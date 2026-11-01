// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters.when_round_tripping;

public class with_an_event_sequence : Specification
{
    Concepts.Observation.FailedPartition _result;

    void Because() => _result = new Concepts.Observation.FailedPartition { EventSequenceId = new("A") }.ToEntity().ToFailedPartition();

    [Fact] void should_keep_the_sequence() => _result.EventSequenceId.ShouldEqual(new EventSequenceId("A"));
}
