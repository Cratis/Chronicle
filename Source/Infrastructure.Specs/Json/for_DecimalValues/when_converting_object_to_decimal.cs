// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_object_to_decimal : Specification
{
    [Fact] void should_round_a_double() => decimal.GetBits(DecimalValues.ToDecimal(1.23d)).ShouldEqual(decimal.GetBits(1.23m));
    [Fact] void should_round_a_float() => decimal.GetBits(DecimalValues.ToDecimal(1.1f)).ShouldEqual(decimal.GetBits(1.1m));
    [Fact] void should_convert_an_integer() => DecimalValues.ToDecimal(42).ShouldEqual(42m);
    [Fact] void should_convert_a_long() => DecimalValues.ToDecimal(long.MaxValue).ShouldEqual(9223372036854775807m);
    [Fact] void should_parse_a_string_with_the_invariant_culture() => DecimalValues.ToDecimal("1.5").ShouldEqual(1.5m);
    [Fact] void should_keep_a_decimal() => DecimalValues.ToDecimal(1.25m).ShouldEqual(1.25m);
    [Fact] void should_convert_an_enum_to_its_underlying_value() => DecimalValues.ToDecimal(DayOfWeek.Tuesday).ShouldEqual(2m);
    [Fact] void should_convert_null_to_zero() => DecimalValues.ToDecimal(null).ShouldEqual(0m);
}
