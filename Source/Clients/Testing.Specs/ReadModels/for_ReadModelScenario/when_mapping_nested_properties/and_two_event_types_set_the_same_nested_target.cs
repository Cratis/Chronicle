// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_two_event_types_set_the_same_nested_target : Specification
{
    ReadModelScenario<NestedPiece> _scenario;
    string _originalTopic;

    void Establish() => _scenario = new();

    async Task Because()
    {
        var id = EventSourceId.New();
        await _scenario.Given.ForEventSource(id).Events(new ContentBriefed(new("Event sourcing"), "Ada"));
        _originalTopic = _scenario.Instance!.Meta.Topic;
        await _scenario.Given.ForEventSource(id).Events(new ContentRebriefed(new("Projections")), new DraftEdited(2));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_apply_the_first_event_type() => _originalTopic.ShouldEqual("Event sourcing");
    [Fact] void should_apply_the_second_event_type() => _scenario.Instance!.Meta.Topic.ShouldEqual("Projections");
    [Fact] void should_apply_the_unrelated_revision() => _scenario.Instance!.Revision.ShouldEqual(2);
}
