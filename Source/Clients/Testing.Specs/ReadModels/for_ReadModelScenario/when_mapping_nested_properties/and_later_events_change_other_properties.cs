// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_mapping_nested_properties;

public class and_later_events_change_other_properties : Specification
{
    ReadModelScenario<ReviewPiece> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new();
        _id = EventSourceId.New();
    }

    async Task Because() => await _scenario.Given.ForEventSource(_id).Events(
        new ContentBriefed(new("Event sourcing"), "Ada"),
        new DraftGenerated(1),
        new DraftEdited(2));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_retain_the_nested_topic() => _scenario.Instance!.Topic.ShouldEqual("Event sourcing");
    [Fact] void should_retain_the_author() => _scenario.Instance!.Author.ShouldEqual("Ada");
    [Fact] void should_map_the_identity() => _scenario.Instance!.Id.ShouldEqual(Guid.Parse(_id.Value));
    [Fact] void should_have_the_latest_revision() => _scenario.Instance!.Revision.ShouldEqual(2);
}
