// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_single_to_rounded_decimal : Specification
{
    Exception _tooLarge;
    Exception _notANumber;

    void Because()
    {
        _tooLarge = Catch.Exception(() => DecimalValues.FromSingleRounded(float.MaxValue));
        _notANumber = Catch.Exception(() => DecimalValues.FromSingleRounded(float.NaN));
    }

    [Fact] void should_round_1_1f_to_1_1() => decimal.GetBits(DecimalValues.FromSingleRounded(1.1f)).ShouldEqual(decimal.GetBits(1.1m));
    [Fact] void should_keep_7_significant_digits() => decimal.GetBits(DecimalValues.FromSingleRounded(16777217f)).ShouldEqual(decimal.GetBits(16777220m));
    [Fact] void should_convert_negative_zero_to_positive_zero() => decimal.GetBits(DecimalValues.FromSingleRounded(-0f)).ShouldEqual(decimal.GetBits(0m));
    [Fact] void should_throw_overflow_for_values_beyond_the_decimal_range() => _tooLarge.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_nan() => _notANumber.ShouldBeOfExactType<OverflowException>();
}
