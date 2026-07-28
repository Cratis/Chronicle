// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class multiple_events_of_the_same_type : given.a_serializer_for_appended_events
{
    void Establish() => SchemaLookupReturns();

    void Because()
    {
        Serialize(AnEvent());
        Serialize(AnEvent());
        Serialize(AnEvent());
    }

    [Fact] void should_look_up_the_schema_only_once() => SchemaLookups().ShouldEqual(1);
}
