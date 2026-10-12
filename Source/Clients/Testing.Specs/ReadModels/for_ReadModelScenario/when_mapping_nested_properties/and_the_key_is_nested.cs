// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_key_is_nested : Specification
{
    ReadModelScenario<PathSummary> _scenario;
    Guid _firstId;
    Guid _secondId;

    void Establish()
    {
        _firstId = Guid.NewGuid();
        _secondId = Guid.NewGuid();
        _scenario = new ReadModelScenario<PathSummary>().WithProjection(builder => builder
            .NoAutoMap()
            .From<PathBriefed>(from => from
                .UsingKey(e => e.Brief.Id)
                .Set(model => model.Id).To(e => e.Brief.Id)
                .Set(model => model.Topic).To(e => e.Brief.Topic)));
    }

    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new PathBriefed(new(_firstId, "First")),
        new PathBriefed(new(_secondId, "Second")));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_two_distinct_keys() => _scenario.Instances.Count.ShouldEqual(2);
    [Fact] void should_map_the_first_key() => _scenario.InstanceForEventSourceId(new EventSourceId(_firstId))!.Topic.ShouldEqual("First");
    [Fact] void should_map_the_second_key() => _scenario.InstanceForEventSourceId(new EventSourceId(_secondId))!.Topic.ShouldEqual("Second");
}
