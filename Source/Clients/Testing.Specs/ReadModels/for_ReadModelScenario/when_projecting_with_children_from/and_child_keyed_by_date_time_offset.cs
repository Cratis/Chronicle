// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_date_time_offset : Specification
{
    ReadModelScenario<MomentLedger> _scenario;
    EventSourceId _ledgerId;
    DateTimeOffset _first;
    DateTimeOffset _second;

    void Establish()
    {
        _scenario = new ReadModelScenario<MomentLedger>();
        _ledgerId = new EventSourceId(Guid.NewGuid());
        _first = new DateTimeOffset(2026, 6, 1, 9, 0, 0, TimeSpan.Zero);
        _second = new DateTimeOffset(2026, 6, 2, 13, 30, 0, TimeSpan.Zero);
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(
                new LedgerOpened("Operations"),
                new MomentAmountRecorded(_first, 1.5m),
                new MomentAmountRecorded(_second, 2.25m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_moment_lines() => _scenario.Instance!.Moments.Count().ShouldEqual(2);
    [Fact] void should_map_first_moment_key() => _scenario.Instance!.Moments.First().Moment.ShouldEqual(_first);
    [Fact] void should_map_first_moment_amount() => _scenario.Instance!.Moments.First().Amount.ShouldEqual(1.5m);
    [Fact] void should_map_second_moment_key() => _scenario.Instance!.Moments.Last().Moment.ShouldEqual(_second);
}
