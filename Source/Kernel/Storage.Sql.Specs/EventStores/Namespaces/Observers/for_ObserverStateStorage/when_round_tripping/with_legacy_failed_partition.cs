// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.EntityFrameworkCore;

using KernelFailedPartition = Cratis.Chronicle.Concepts.Observation.FailedPartition;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_legacy_failed_partition : given.an_observer_state_storage
{
    KernelFailedPartition _restored;
    string _savedJson;

    async Task Establish()
    {
        await _storage.Save(new() { Identifier = "observer" });
        const string partitions = """
            [{"Id":"11111111-1111-1111-1111-111111111111","Partition":"partition","ObserverId":"observer","Attempts":[{"Occurred":"2026-10-17T12:00:00+00:00","SequenceNumber":42,"Messages":[],"StackTrace":""}],"IsResolved":false}]
            """;
        await using var context = CreateContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE Observers SET FailedPartitions = {partitions} WHERE Id = 'observer'");
    }

    async Task Because()
    {
        var state = await _storage.Get("observer");
        _restored = state.FailedPartitions.Single();
        await _storage.Save(state);
        await using var context = CreateContext();
        _savedJson = await context.Database.SqlQueryRaw<string>("SELECT FailedPartitions AS Value FROM Observers WHERE Id = 'observer'").SingleAsync();
    }

    [Fact] void should_not_initialize_a_cursor() => _restored.RecoveryCursor.ShouldBeNull();
    [Fact] void should_not_assign_an_owner() => _restored.RecoveryOwner.ShouldBeNull();
    [Fact] void should_not_invent_a_hold() => _restored.Hold.ShouldBeNull();
    [Fact] void should_interpret_the_last_attempt_as_live() => _restored.EffectiveRecoveryCursor.ShouldEqual(new(new(42), FailedPartitionRecoveryMode.Live, null));
    [Fact] void should_keep_the_retry_budget() => _restored.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_default_to_not_quarantined() => _restored.IsQuarantined.ShouldBeFalse();
    [Fact] void should_default_to_no_attempts_before_a_budget_reset() => _restored.AttemptsBeforeBudgetReset.ShouldEqual(0);
    [Fact] void should_not_write_recovery_state() => _savedJson.Contains("Recovery", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_not_write_a_hold() => _savedJson.Contains("Hold", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
}
