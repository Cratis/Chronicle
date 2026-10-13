// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_the_target_is_nested : Specification
{
    ReadModelScenario<NestedPiece> _scenario;

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(
        new ContentBriefed(new("Event sourcing"), "Ada"),
        new DraftGenerated(1),
        new DraftEdited(2));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_have_the_metadata_object() => _scenario.Instance!.Meta.ShouldNotBeNull();
    [Fact] void should_retain_the_nested_topic() => _scenario.Instance!.Meta.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_have_the_latest_revision() => _scenario.Instance!.Revision.ShouldEqual(2);
}
