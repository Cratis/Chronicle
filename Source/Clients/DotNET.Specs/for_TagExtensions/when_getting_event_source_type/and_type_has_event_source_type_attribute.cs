// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_TagExtensions.when_getting_event_source_type;

public class and_type_has_event_source_type_attribute : Specification
{
    [EventSourceType("order")]
    class TypeWithEventSourceType;

    EventSourceType _result;

    void Because() => _result = typeof(TypeWithEventSourceType).GetEventSourceType();

    [Fact] void should_return_the_event_source_type() => _result.ShouldEqual(new EventSourceType("order"));
}
