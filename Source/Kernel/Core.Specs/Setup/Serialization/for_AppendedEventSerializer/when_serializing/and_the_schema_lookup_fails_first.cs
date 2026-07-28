// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class and_the_schema_lookup_fails_first : given.a_serializer_for_appended_events
{
    Exception _error;
    byte[] _result;

    void Establish() =>
        _eventTypesStorage
            .GetFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>())
            .Returns(
                _ => Task.FromException<EventTypeSchema>(new InvalidOperationException("Schema lookup failed")),
                _ => Task.FromResult(_schema));

    void Because()
    {
        _error = Catch.Exception(() => Serialize(AnEvent()));
        _result = Serialize(AnEvent());
    }

    [Fact] void should_fail_the_first_serialization() => _error.ShouldNotBeNull();
    [Fact] void should_serialize_the_next_event() => _result.ShouldNotBeEmpty();
    [Fact] void should_look_the_schema_up_again() => SchemaLookups().ShouldEqual(2);
}
