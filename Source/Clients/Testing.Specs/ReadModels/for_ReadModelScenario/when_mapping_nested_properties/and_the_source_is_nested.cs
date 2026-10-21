// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_source_is_nested : Specification
{
    ReadModelScenario<ReviewPiece> _scenario;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ContentBriefed(new("Event sourcing"), "Ada"));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_map_the_nested_topic() => _scenario.Instance!.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_map_the_author() => _scenario.Instance!.Author.ShouldEqual("Ada");
}
