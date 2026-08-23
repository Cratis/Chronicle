// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_referenced_type_is_not_an_event_type : Specification
{
    record NotAnEventType(string Name);

    [EventTypeGenerationFor<NotAnEventType>(1)]
    record MyEventV1(string Name);

    Exception _error;

    void Because() => _error = Catch.Exception(() => typeof(MyEventV1).GetEventType());

    [Fact] void should_throw_event_type_generation_references_non_event_type() => _error.ShouldBeOfExactType<EventTypeGenerationReferencesNonEventType>();
}
