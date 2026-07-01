// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

/// <summary>
/// Verifies that class-level <c>[NoAutoMap]</c> still fully disables name-based AutoMap alongside the
/// explicit-beats-implicit merge: the explicitly-sourced property is populated while a name-matching
/// property with no explicit source is left unset.
/// </summary>
public class when_class_level_auto_map_is_disabled : Specification
{
    ReadModelScenario<NoAutoMapWorkArrangementSummary> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<NoAutoMapWorkArrangementSummary>();
        _id = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new WorkArrangementSet("Oslo", 5));

    [Fact] void should_set_the_explicitly_sourced_property() => _scenario.Instance!.Location.ShouldEqual("Oslo");
    [Fact] void should_not_auto_map_the_name_matching_property() => _scenario.Instance!.WorkMode.ShouldEqual(0);
}
