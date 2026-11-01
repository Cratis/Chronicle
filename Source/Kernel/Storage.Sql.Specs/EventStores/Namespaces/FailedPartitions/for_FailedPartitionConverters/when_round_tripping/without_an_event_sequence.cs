// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters.when_round_tripping;

public class without_an_event_sequence : Specification
{
    Concepts.Observation.FailedPartition _result;
    JsonElement _document;

    void Because()
    {
        var entity = new Concepts.Observation.FailedPartition().ToEntity();
        _document = JsonSerializer.Deserialize<JsonElement>(entity.StateJson);
        _result = entity.ToFailedPartition();
    }

    [Fact] void should_not_write_a_sequence_property() => _document.TryGetProperty("EventSequenceId", out _).ShouldBeFalse();
    [Fact] void should_leave_legacy_failures_untagged() => _result.EventSequenceId.ShouldBeNull();
}
