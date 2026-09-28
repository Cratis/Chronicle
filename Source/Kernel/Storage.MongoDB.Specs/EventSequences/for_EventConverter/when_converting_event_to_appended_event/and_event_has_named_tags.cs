// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_event_has_named_tags : given.an_event_converter
{
    AppendedEvent _result;

    void Establish() => _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);

    async Task Because() => _result = await _converter.ToAppendedEvent(CreateEvent() with
    {
        NamedTags = [new NamedTagDocument("account", "one"), new NamedTagDocument("account", "two")]
    });

    [Fact] void should_preserve_names_and_values() => _result.Context.NamedTags.ShouldContainOnly(
        new NamedTag(new TagName("account"), "one"), new NamedTag(new TagName("account"), "two"));
}
