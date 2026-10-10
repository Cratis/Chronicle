// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_from_double : Specification
{
    decimal _result;

    void Because() => _result = DecimalValues.FromDouble(193.58d);

    [Fact] void should_use_the_shortest_round_trip_text() => decimal.GetBits(_result).ShouldEqual(decimal.GetBits(193.58m));
}
