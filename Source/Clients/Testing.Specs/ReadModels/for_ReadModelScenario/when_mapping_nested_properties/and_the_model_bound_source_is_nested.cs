// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_model_bound_source_is_nested : Specification
{
    ReadModelScenario<ModelBoundPathSummary> _scenario;

    void Establish() => _scenario = new();
    Task Because() => _scenario.Given.ForEventSource(EventSourceId.New()).Events(new PathBriefed(new(Guid.NewGuid(), "Nested source")));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_map_the_dotted_set_from_source() => _scenario.Instance!.Topic.ShouldEqual("Nested source");
}
