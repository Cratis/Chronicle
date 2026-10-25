// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_generational_content;

public class and_a_generation_schema_is_missing : given.an_event_with_protected_history
{
    void Establish()
    {
        _schemas.Remove(new EventType("person", 2));
        _schemas.Remove(_event.Context.EventType);
    }

    async Task Because() => _result = await _compliance.Release([_event], _schemas);

    [Fact] void should_leave_current_content_without_a_schema_unchanged() => _result[0].Content.ShouldBeSame(_event.Content);
    [Fact] void should_not_substitute_another_generations_schema() => _result[0].GenerationalContent[2].ShouldEqual(_event.GenerationalContent[2]);
    [Fact] void should_leave_revision_without_a_schema_unchanged() => _result[0].Revisions.Single().Content.ShouldEqual(_event.Revisions.Single().Content);
    [Fact] void should_still_release_available_generations() => Value(_result[0].GenerationalContent[1], 1).ShouldEqual("released");
    [Fact] void should_still_release_original_content() => Value(_result[0].OriginalContent, 1).ShouldEqual("released");
}
