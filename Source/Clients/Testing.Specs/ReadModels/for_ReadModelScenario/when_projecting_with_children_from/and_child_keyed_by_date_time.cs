// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_date_time : Specification
{
    ReadModelScenario<StampLedger> _scenario;
    EventSourceId _ledgerId;
    DateTime _first;
    DateTime _second;

    void Establish()
    {
        _scenario = new ReadModelScenario<StampLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
        _first = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        _second = new DateTime(2026, 6, 2, 13, 30, 0, DateTimeKind.Utc);
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new StampAmountRecorded(_first, 1.5m),
                new StampAmountRecorded(_second, 2.25m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_stamp_lines() => _scenario.Instance!.Stamps.Count().ShouldEqual(2);
    [Fact] void should_map_first_stamp_key() => _scenario.Instance!.Stamps.First().Stamp.ShouldEqual(_first);
    [Fact] void should_map_first_stamp_amount() => _scenario.Instance!.Stamps.First().Amount.ShouldEqual(1.5m);
    [Fact] void should_map_second_stamp_key() => _scenario.Instance!.Stamps.Last().Stamp.ShouldEqual(_second);
}
