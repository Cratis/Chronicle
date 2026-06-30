// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_child_keyed_by_date_only : Specification
{
    ReadModelScenario<Sheet> _scenario;
    SheetId _sheetId;

    void Establish()
    {
        _scenario = new ReadModelScenario<Sheet>();
        _sheetId = SheetId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sheetId)
            .Events(
                new SheetStarted(2026),
                new DayWorked(new DateOnly(2026, 6, 1), 7.5m),
                new DayWorked(new DateOnly(2026, 6, 2), 8m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_have_two_day_entries() => _scenario.Instance!.Days.Count().ShouldEqual(2);
    [Fact] void should_map_first_day() => _scenario.Instance!.Days.First().Day.ShouldEqual(new DateOnly(2026, 6, 1));
    [Fact] void should_map_first_day_hours() => _scenario.Instance!.Days.First().Hours.ShouldEqual(7.5m);
    [Fact] void should_map_second_day() => _scenario.Instance!.Days.Last().Day.ShouldEqual(new DateOnly(2026, 6, 2));
    [Fact] void should_map_second_day_hours() => _scenario.Instance!.Days.Last().Hours.ShouldEqual(8m);
}
