// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_event_has_explicit_subject : given.an_event_converter
{
    static readonly Subject _subject = new("explicit-subject-id");
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        _event = CreateEvent(_subject);
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_set_subject_on_context() => _result.Context.Subject.ShouldEqual(_subject);
    [Fact] void should_not_be_subject_is_event_source_id() => _result.Context.SubjectIsEventSourceId.ShouldBeFalse();
}
