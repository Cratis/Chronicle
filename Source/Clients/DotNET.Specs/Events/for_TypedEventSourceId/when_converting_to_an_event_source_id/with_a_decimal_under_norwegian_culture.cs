// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_converting_to_an_event_source_id;

public class with_a_decimal_under_norwegian_culture : given.a_norwegian_culture
{
    EventSourceId _result;

    void Because() => _result = new EventSourceId<decimal>(123.45m);

    [Fact] void should_use_the_invariant_decimal_separator() => _result.Value.ShouldEqual("123.45");
}
