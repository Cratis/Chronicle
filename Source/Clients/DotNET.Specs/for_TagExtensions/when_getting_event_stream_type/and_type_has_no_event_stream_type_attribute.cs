// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_TagExtensions.when_getting_event_stream_type;

public class and_type_has_no_event_stream_type_attribute : Specification
{
    class TypeWithoutEventStreamType;

    EventStreamType _result;

    void Because() => _result = typeof(TypeWithoutEventStreamType).GetEventStreamType();

    [Fact] void should_return_all() => _result.ShouldEqual(EventStreamType.All);
}
