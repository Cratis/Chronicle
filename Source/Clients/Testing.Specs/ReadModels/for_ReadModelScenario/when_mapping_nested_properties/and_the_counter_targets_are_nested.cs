// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_counter_targets_are_nested : Specification
{
    ReadModelScenario<PathTotals> _scenario;

    void Establish() => _scenario = new ReadModelScenario<PathTotals>().WithProjection(builder => builder
        .NoAutoMap()
        .From<PathBriefed>(from => from
            .Count(model => model.Statistics.Count)
            .Increment(model => model.Statistics.Changes)));

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new PathBriefed(new(Guid.NewGuid(), "First")),
        new PathBriefed(new(Guid.NewGuid(), "Second")));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_count_at_the_nested_target() => _scenario.Instance!.Statistics.Count.ShouldEqual(2);
    [Fact] void should_increment_at_the_nested_target() => _scenario.Instance!.Statistics.Changes.ShouldEqual(2);
}
