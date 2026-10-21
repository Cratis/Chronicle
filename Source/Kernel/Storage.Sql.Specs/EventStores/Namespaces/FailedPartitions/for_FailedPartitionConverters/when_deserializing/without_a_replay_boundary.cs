// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters.when_deserializing;

public class without_a_replay_boundary : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => JsonSerializer.Deserialize<FailedPartitionRecoveryCursor>("""{"Position":{"Value":51},"Mode":1,"ReplayBoundary":null}"""));

    [Fact] void should_refuse_the_invalid_cursor() => _error.ShouldBeOfExactType<InvalidFailedPartitionRecoveryCursor>();
}
