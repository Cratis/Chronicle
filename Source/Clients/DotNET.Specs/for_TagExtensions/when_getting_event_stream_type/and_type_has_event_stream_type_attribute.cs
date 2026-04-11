// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.for_TagExtensions.when_getting_event_stream_type;

public class and_type_has_event_stream_type_attribute : Specification
{
    [EventStreamType("invoices")]
    class TypeWithEventStreamType;

    EventStreamType _result;

    void Because() => _result = typeof(TypeWithEventStreamType).GetEventStreamType();

    [Fact] void should_return_the_event_stream_type() => _result.ShouldEqual(new EventStreamType("invoices"));
}
