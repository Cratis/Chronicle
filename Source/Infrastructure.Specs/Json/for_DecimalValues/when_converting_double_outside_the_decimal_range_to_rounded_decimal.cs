// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_double_outside_the_decimal_range_to_rounded_decimal : Specification
{
    Exception _tooLarge;
    Exception _tooSmall;
    Exception _notANumber;
    Exception _infinity;

    void Because()
    {
        _tooLarge = Catch.Exception(() => DecimalValues.FromDoubleRounded(1e29d));
        _tooSmall = Catch.Exception(() => DecimalValues.FromDoubleRounded(-1e29d));
        _notANumber = Catch.Exception(() => DecimalValues.FromDoubleRounded(double.NaN));
        _infinity = Catch.Exception(() => DecimalValues.FromDoubleRounded(double.PositiveInfinity));
    }

    [Fact] void should_throw_overflow_for_1e29() => _tooLarge.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_minus_1e29() => _tooSmall.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_nan() => _notANumber.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_infinity() => _infinity.ShouldBeOfExactType<OverflowException>();
}
