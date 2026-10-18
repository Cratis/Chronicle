// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

public abstract partial class DefinitionChangeRowsContract
{
    [Fact] public async Task should_accept_a_first_checkpoint_in_started_state() => await RoundTrip(Progress());
    [Fact] public async Task should_reject_progress_in_intended_state() => (await OpenRows<ObserverProgressRecord>().TryInsert(Progress() with { State = ObserverDefinitionChangeRowState.Intended })).ShouldBeFalse();
    [Fact] public async Task should_accept_a_step_terminal_record_without_a_prior_checkpoint()
    {
        var row = Progress();
        await RoundTrip(row with { State = ObserverDefinitionChangeRowState.Terminal, Data = row.Data with { Outcome = ObserverWorkOutcome.Success } });
    }
    [Fact] public async Task should_reject_inventory_inserted_as_started() => (await OpenRows<ObserverJobInventory>().TryInsert(Inventory() with { State = ObserverDefinitionChangeRowState.Started })).ShouldBeFalse();
    [Fact] public async Task should_reject_inventory_inserted_as_terminal()
    {
        var row = Inventory();
        (await OpenRows<ObserverJobInventory>().TryInsert(row with { State = ObserverDefinitionChangeRowState.Terminal, Data = row.Data with { Outcome = new(ObserverWorkOutcome.Success, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, true) } })).ShouldBeFalse();
    }
    [Fact] public async Task should_keep_a_completion_that_arrives_inside_start()
    {
        var row = Inventory();
        await Insert(row);
        var terminal = row with { Version = 1, State = ObserverDefinitionChangeRowState.Terminal, Data = row.Data with { Outcome = new(ObserverWorkOutcome.Success, EventSequenceNumber.Unavailable, EventSequenceNumber.Unavailable, true) } };
        (await OpenRows<ObserverJobInventory>().TryReplace(terminal, 0)).ShouldBeTrue();
        (await OpenRows<ObserverJobInventory>().TryReplace(row with { Version = 2, State = ObserverDefinitionChangeRowState.Started }, 1)).ShouldBeFalse();
        ObserverDefinitionChangeRows.Serialize((await OpenRows<ObserverJobInventory>().Get(row.Key))!).ShouldEqual(ObserverDefinitionChangeRows.Serialize(terminal));
    }
    [Fact] public async Task should_not_rewrite_terminal_progress()
    {
        var row = Progress();
        await Insert(row);
        var terminal = row with { Version = 1, State = ObserverDefinitionChangeRowState.Terminal, Data = row.Data with { Outcome = ObserverWorkOutcome.Held } };
        (await OpenRows<ObserverProgressRecord>().TryReplace(terminal, 0)).ShouldBeTrue();
        (await OpenRows<ObserverProgressRecord>().TryReplace(terminal with { Version = 2 }, 1)).ShouldBeFalse();
    }
    [Fact] public async Task should_round_trip_a_terminal_failure_with_recovery_state()
    {
        var row = Inventory();
        await Insert(row);
        var failure = new FailedPartition
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(71), FailedPartitionRecoveryMode.Replay, new(100)),
            RecoveryOwner = new(Guid.NewGuid(), _job.Value, 3, FailedPartitionRecoveryState.Resuming, 1),
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), DateTimeOffset.UtcNow),
            Attempts = [new() { SequenceNumber = new(70), Kind = FailureKind.Handling }],
            IsQuarantined = true
        };
        var terminal = row with { Version = 1, State = ObserverDefinitionChangeRowState.Terminal, Data = row.Data with { Outcome = new(ObserverWorkOutcome.Held, new(70), new(71), false, failure) } };
        (await OpenRows<ObserverJobInventory>().TryReplace(terminal, 0)).ShouldBeTrue();
        ObserverDefinitionChangeRows.Serialize((await OpenRows<ObserverJobInventory>().Get(row.Key))!).ShouldEqual(ObserverDefinitionChangeRows.Serialize(terminal));
    }
}
