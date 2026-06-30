// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_time_only : Specification
{
    ReadModelScenario<TimeLedger> _scenario;
    EventSourceId _ledgerId;

    void Establish()
    {
        _scenario = new ReadModelScenario<TimeLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new SlotAmountRecorded(new TimeOnly(9, 0), 1.5m),
                new SlotAmountRecorded(new TimeOnly(13, 30), 2.25m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_slot_lines() => _scenario.Instance!.Slots.Count().ShouldEqual(2);
    [Fact] void should_map_first_slot_key() => _scenario.Instance!.Slots.First().Slot.ShouldEqual(new TimeOnly(9, 0));
    [Fact] void should_map_first_slot_amount() => _scenario.Instance!.Slots.First().Amount.ShouldEqual(1.5m);
    [Fact] void should_map_second_slot_key() => _scenario.Instance!.Slots.Last().Slot.ShouldEqual(new TimeOnly(13, 30));
}
