// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters.when_serializing;

public class with_legacy_state : Specification
{
    JsonElement _saved;

    void Because() => _saved = JsonSerializer.Deserialize<JsonElement>(new Concepts.Observation.FailedPartition().ToEntity().StateJson);

    [Fact] void should_not_write_a_deferral() => _saved.TryGetProperty("Deferral", out _).ShouldBeFalse();
    [Fact] void should_not_write_a_recovery_cursor() => _saved.TryGetProperty("RecoveryCursor", out _).ShouldBeFalse();
    [Fact] void should_not_write_a_recovery_owner() => _saved.TryGetProperty("RecoveryOwner", out _).ShouldBeFalse();
    [Fact] void should_not_write_a_hold() => _saved.TryGetProperty("Hold", out _).ShouldBeFalse();
    [Fact] void should_not_write_the_derived_cursor() => _saved.TryGetProperty("EffectiveRecoveryCursor", out _).ShouldBeFalse();
}
