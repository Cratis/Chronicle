// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Verifies that a <see cref="DateOnly"/>-keyed child collection whose children live on their own event
/// source (linked to the parent via an explicit parent key) materializes correctly — exercising the sink
/// parent-hierarchy lookup that matches a child by its <see cref="DateOnly"/> key value.
/// </summary>
public class and_child_keyed_by_date_only_on_separate_event_sources : Specification
{
    ReadModelScenario<DayLedger> _scenario;
    EventSourceId _ledgerId;
    Guid _ledgerGuid;
    DateOnly _firstDay;
    DateOnly _secondDay;

    void Establish()
    {
        _scenario = new ReadModelScenario<DayLedger>();
        _ledgerGuid = Guid.NewGuid();
        _ledgerId = new EventSourceId(_ledgerGuid);
        _firstDay = new DateOnly(2026, 6, 1);
        _secondDay = new DateOnly(2026, 6, 2);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_ledgerId)
            .Events(new LedgerOpened("Operations"));

        await _scenario.Given
            .ForEventSource(new EventSourceId(Guid.NewGuid()))
            .Events(new DayRaised(_ledgerGuid, _firstDay, 100m));

        await _scenario.Given
            .ForEventSource(new EventSourceId(Guid.NewGuid()))
            .Events(new DayRaised(_ledgerGuid, _secondDay, 200m));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_day_lines() => _scenario.Instance!.Days.Count().ShouldEqual(2);
    [Fact] void should_map_first_day_key() => _scenario.Instance!.Days.First().Day.ShouldEqual(_firstDay);
    [Fact] void should_map_first_day_amount() => _scenario.Instance!.Days.First().Amount.ShouldEqual(100m);
    [Fact] void should_map_second_day_key() => _scenario.Instance!.Days.Last().Day.ShouldEqual(_secondDay);
}
