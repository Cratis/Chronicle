// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_double_to_rounded_decimal : Specification
{
    static void ShouldBeIdentical(decimal actual, decimal expected) => decimal.GetBits(actual).ShouldEqual(decimal.GetBits(expected));

    [Fact] void should_round_1_23_to_1_23() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(1.23d), 1.23m);
    [Fact] void should_round_0_1_to_0_1() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(0.1d), 0.1m);
    [Fact] void should_round_the_sum_of_0_1_and_0_2_to_0_3() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(0.1d + 0.2d), 0.3m);
    [Fact] void should_keep_15_significant_digits_of_2_to_the_power_of_53_minus_1() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(9007199254740991d), 9007199254740990m);
    [Fact] void should_convert_1e28() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(1e28d), 10000000000000000000000000000m);
    [Fact] void should_convert_7_9e28() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(7.9e28d), 79000000000000000000000000000m);
    [Fact] void should_convert_1e_minus_10() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(1e-10d), 0.0000000001m);
    [Fact] void should_convert_1e_minus_30_to_zero() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(1e-30d), 0m);
    [Fact] void should_convert_negative_values() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(-1.5d), -1.5m);
    [Fact] void should_convert_negative_zero_to_positive_zero() => ShouldBeIdentical(DecimalValues.FromDoubleRounded(-0d), 0m);
}
