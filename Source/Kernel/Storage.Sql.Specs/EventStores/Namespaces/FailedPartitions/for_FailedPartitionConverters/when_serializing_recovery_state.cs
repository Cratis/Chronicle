// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters;

public class when_serializing_recovery_state : Specification
{
    JsonElement _json;

    void Because()
    {
        var partition = new Concepts.Observation.FailedPartition
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(51), FailedPartitionRecoveryMode.Replay, new(80)),
            RecoveryOwner = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("22222222-2222-2222-2222-222222222222"), 3, FailedPartitionRecoveryState.Resuming, 7)
        };
        using var document = JsonDocument.Parse(partition.ToEntity().StateJson);
        _json = document.RootElement.Clone();
    }

    [Fact] void should_store_existing_sequence_concepts_as_objects() => _json.GetProperty("RecoveryCursor").GetProperty("Position").ValueKind.ShouldEqual(JsonValueKind.Object);
    [Fact] void should_keep_the_sequence_value_inside_the_concept() => _json.GetProperty("RecoveryCursor").GetProperty("Position").GetProperty("Value").GetUInt64().ShouldEqual(51UL);
    [Fact] void should_store_the_owner_job_id_as_a_string() => _json.GetProperty("RecoveryOwner").GetProperty("JobId").GetString().ShouldEqual("22222222-2222-2222-2222-222222222222");
    [Fact] void should_store_the_owner_revision_as_a_number() => _json.GetProperty("RecoveryOwner").GetProperty("Revision").GetInt64().ShouldEqual(7L);
}
