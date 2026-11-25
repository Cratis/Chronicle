// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateConverters;

public class when_round_tripping_pending_catch_up_repairs : Specification
{
    KernelObserverState _result;
    KernelObserverState _legacy;
    bool _comparedAsEqual;

    void Because()
    {
        var row = (KernelObserverState.Empty with { Identifier = "observer", NextEventSequenceNumber = 13UL, PendingCatchUpRepairs = [new("B", 10UL, 13UL)] }).ToSql();
        _result = row.ToKernel();
        _legacy = new ObserverState { Id = "legacy" }.ToKernel();
        var withoutRepair = (KernelObserverState.Empty with { Identifier = "observer", NextEventSequenceNumber = 13UL }).ToSql();
        _comparedAsEqual = new ObserverStateRowComparer().Equals(row, withoutRepair);
    }

    [Fact] void should_preserve_the_range() => _result.PendingCatchUpRepairs!.ShouldContainOnly(new Observation.PendingCatchUpRepair("B", 10UL, 13UL));
    [Fact] void should_treat_a_null_legacy_column_as_no_repairs() => _legacy.PendingCatchUpRepairs.ShouldBeNull();
    [Fact] void should_not_coalesce_away_a_repair_only_change() => _comparedAsEqual.ShouldBeFalse();
}
