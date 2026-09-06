// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3940 — the event that creates a child entry
/// applied the child's property mappers twice, which is invisible for a plain set but doubles an
/// accumulating <c>[AddFrom]</c>. The root-level accumulation on the same event must stay correct too.
/// </summary>
public class and_a_child_property_accumulates_with_add_from : Specification
{
    ReadModelScenario<UsageLedger> _scenario;
    Guid _ledgerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<UsageLedger>();
        _ledgerId = Guid.NewGuid();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Capacity"),
                new UsageRecorded("2026-09", 12.5m),
                new UsageRecorded("2026-09", 5m),
                new UsageRecorded("2026-10", 2m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_accumulate_the_root_total() => _scenario.Instance.TotalConsumed.ShouldEqual(19.5m);
    [Fact] void should_have_a_period_per_distinct_key() => _scenario.Instance.Periods.Count().ShouldEqual(2);

    [Fact] void should_accumulate_the_created_period_exactly_once() =>
        _scenario.Instance.Periods.Single(_ => _.Period == "2026-09").Consumed.ShouldEqual(17.5m);

    [Fact] void should_accumulate_a_period_created_by_a_later_event_exactly_once() =>
        _scenario.Instance.Periods.Single(_ => _.Period == "2026-10").Consumed.ShouldEqual(2m);
}
