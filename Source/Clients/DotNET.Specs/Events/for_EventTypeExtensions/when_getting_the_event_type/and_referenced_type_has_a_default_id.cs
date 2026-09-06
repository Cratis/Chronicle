// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_the_event_type;

public class and_referenced_type_has_a_default_id : Specification
{
    [EventType(generation: 2)]
    record MyOtherEventV2(string Name);

    [EventTypeGenerationFor<MyOtherEventV2>(1)]
    record MyOtherEventV1(string Name);

    EventType _result;

    void Because() => _result = typeof(MyOtherEventV1).GetEventType();

    [Fact] void should_resolve_the_id_from_the_referenced_types_type_name() => _result.Id.Value.ShouldEqual(nameof(MyOtherEventV2));
}
