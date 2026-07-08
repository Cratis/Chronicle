// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_a_stream_that_carries_an_unsubscribed_event;

/// <summary>
/// Seeds a <c>[ChildrenFrom]</c> projection's event source with the root event, its child events, and an
/// unsubscribed audit/marker event. This guards that the skip predicate (<c>HasKeyResolverFor</c>) still treats
/// child event types as subscribed — so the child collection materializes — while ignoring only the truly
/// unsubscribed event, leaving previously-working hierarchical scenarios unaffected.
/// </summary>
public class and_projecting_a_children_from_collection : Specification
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
                new ModuleAudited(),
                new DayWorked(new DateOnly(2026, 6, 2), 8m));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_the_root_year() => _scenario.Instance!.Year.ShouldEqual(2026);
    [Fact] void should_have_two_day_entries() => _scenario.Instance!.Days.Count().ShouldEqual(2);
    [Fact] void should_map_first_day() => _scenario.Instance!.Days.First().Day.ShouldEqual(new DateOnly(2026, 6, 1));
    [Fact] void should_map_second_day() => _scenario.Instance!.Days.Last().Day.ShouldEqual(new DateOnly(2026, 6, 2));
}
