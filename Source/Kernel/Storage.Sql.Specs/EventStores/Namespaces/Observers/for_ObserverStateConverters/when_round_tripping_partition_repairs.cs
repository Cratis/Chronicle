// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateConverters;

public class when_round_tripping_partition_repairs : Specification
{
    static readonly Observation.PartitionRepairState _repair = new(
        "partition",
        new(42, true, [new("chosen", EventTypeGeneration.First)]),
        12,
        null,
        new(new(Guid.NewGuid()), new(4)),
        [new(EventSequenceNumber.Max, false, [])],
        CountsPrepared: true);
    Observation.ObserverState _result;
    Observation.ObserverState _legacy;
    ObserverState _cleared;

    void Because()
    {
        var state = Observation.ObserverState.Empty with { Identifier = "observer", PartitionRepairs = [_repair] };
        _result = state.ToSql().ToKernel();
        _legacy = new ObserverState { Id = "legacy" }.ToKernel();
        _cleared = (state with { PartitionRepairs = null }).ToSql();
    }

    [Fact] void should_keep_the_held_back_position() => _result.PartitionRepairs!.Single().HeldBackFrom.ShouldEqual(_repair.HeldBackFrom);
    [Fact] void should_keep_the_exact_retiring_job_and_epoch() => _result.PartitionRepairs!.Single().Retiring.ShouldEqual(_repair.Retiring);
    [Fact] void should_keep_coverage_and_generation() => _result.PartitionRepairs!.Single().Head.EventTypes.ShouldEqual(_repair.Head.EventTypes);
    [Fact] void should_keep_different_waiting_coverage() => _result.PartitionRepairs!.Single().Waiting.Single().Through.ShouldEqual(EventSequenceNumber.Max);
    [Fact] void should_treat_a_legacy_row_as_having_no_repairs() => _legacy.PartitionRepairs.ShouldBeNull();
    [Fact] void should_clear_the_column() => _cleared.PartitionRepairs.ShouldBeNull();
}
