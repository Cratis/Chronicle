// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation;

public abstract partial class DefinitionChangeRowsContract
{
    [Fact] public async Task should_reject_a_changed_progress_job() => await RejectIdentityChange(Progress(), Progress().Data with { JobId = Guid.NewGuid() });
    [Fact] public async Task should_reject_a_changed_progress_partition() => await RejectIdentityChange(Progress(), Progress().Data with { Partition = "other" });
    [Fact] public async Task should_reject_a_changed_continuation_job() => await RejectIdentityChange(Continuation(), Continuation().Data with { JobId = Guid.NewGuid() });
    [Fact] public async Task should_reject_a_changed_continuation_partition() => await RejectIdentityChange(Continuation(), Continuation().Data with { Partition = "other" });
    [Fact] public async Task should_reject_a_changed_plan_job() => await RejectIdentityChange(Plan(), Plan().Data with { JobId = Guid.NewGuid() });
    [Fact] public async Task should_reject_a_changed_plan_partition() => await RejectIdentityChange(Plan(), Plan().Data with { Partition = null });
    [Fact] public async Task should_reject_a_changed_inventory_job() => await RejectIdentityChange(Inventory(), Inventory().Data with { JobId = Guid.NewGuid() });
    [Fact] public async Task should_reject_a_changed_inventory_kind() => await RejectIdentityChange(Inventory(), Inventory().Data with { Kind = ObserverDefinitionChangeJobKind.Replay });
    [Fact] public async Task should_reject_a_changed_inventory_job_type() => await RejectIdentityChange(Inventory(), Inventory().Data with { JobType = "ReplayObserver" });
    [Fact] public async Task should_reject_a_changed_inventory_origin() => await RejectIdentityChange(Inventory(), Inventory().Data with { Origin = null });

    async Task RejectIdentityChange<T>(ObserverDefinitionChangeRow<T> row, T changed)
    {
        await Insert(row);
        (await OpenRows<T>().TryReplace(row with { Version = 1, Data = changed }, 0)).ShouldBeFalse();
        ObserverDefinitionChangeRows.Serialize((await OpenRows<T>().Get(row.Key))!).ShouldEqual(ObserverDefinitionChangeRows.Serialize(row));
    }
}
