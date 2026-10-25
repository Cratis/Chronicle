// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_generational_content;

public class and_every_generation_is_protected : given.an_event_with_protected_history
{
    async Task Because() => _result = await _compliance.Release([_event], _schemas);

    [Fact] void should_release_current_content() => ((IDictionary<string, object?>)_result[0].Content)["name3"].ShouldEqual("released");
    [Fact] void should_release_every_generation_with_its_own_schema() => _result[0].GenerationalContent.ShouldEachConformTo(item => Value(item.Value, item.Key) == "released");
    [Fact] void should_release_original_content_with_the_first_schema() => Value(_result[0].OriginalContent, 1).ShouldEqual("released");
    [Fact] void should_release_revision_content_with_its_generation_schema() => Value(_result[0].Revisions.Single().Content, 2).ShouldEqual("released");
    [Fact] void should_preserve_context() => _result[0].Context.ShouldEqual(_event.Context);
    [Fact] void should_preserve_revision_metadata() => (_result[0].Revisions.Single() with { Content = _event.Revisions.Single().Content }).ShouldEqual(_event.Revisions.Single());
    [Fact] void should_not_modify_stored_content() => Value(_event.GenerationalContent[1], 1).ShouldEqual("protected");
}
