// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

/// <summary>
/// Verifies child-identity matching for a <see cref="DateOnly"/> key: two events carrying the SAME
/// <see cref="DateOnly"/> resolve to the SAME child (an update), not two duplicate children — the same
/// behavior as the real runtime sink.
/// </summary>
public class and_two_events_share_a_date_only_child_key : Specification
{
    ReadModelScenario<ConceptSheet> _scenario;
    SheetId _sheetId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ConceptSheet>();
        _sheetId = SheetId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_sheetId)
            .Events(
                new ConceptSheetStarted(2026),
                new ConceptDayWorked(new DateOnly(2026, 6, 1), new WorkHours(7.5m)),
                new ConceptDayWorked(new DateOnly(2026, 6, 1), new WorkHours(9m)));

    [Fact] void should_have_a_single_merged_child() => _scenario.Instance!.Days.Count().ShouldEqual(1);
    [Fact] void should_keep_the_shared_key() => _scenario.Instance!.Days.Single().Day.ShouldEqual(new DateOnly(2026, 6, 1));
    [Fact] void should_apply_the_latest_value() => _scenario.Instance!.Days.Single().Hours.ShouldEqual(new WorkHours(9m));
}
