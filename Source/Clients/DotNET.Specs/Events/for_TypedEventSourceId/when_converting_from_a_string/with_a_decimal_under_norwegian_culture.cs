// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_converting_from_a_string;

public class with_a_decimal_under_norwegian_culture : given.a_norwegian_culture
{
    EventSourceId<decimal> _result;

    void Because() => _result = "123.45";

    [Fact] void should_parse_the_invariant_decimal_separator() => _result.TypedValue.ShouldEqual(123.45m);
}
