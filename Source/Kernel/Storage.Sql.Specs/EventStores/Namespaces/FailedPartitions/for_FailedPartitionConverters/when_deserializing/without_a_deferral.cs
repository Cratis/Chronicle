// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters.when_deserializing;

public class without_a_deferral : Specification
{
    Concepts.Observation.FailedPartition _restored;

    void Because() => _restored = new FailedPartition { StateJson = "{}" }.ToFailedPartition();

    [Fact] void should_read_a_null_deferral() => _restored.Deferral.ShouldBeNull();
}
