// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Json.for_DecimalValues;

public class when_converting_boundary_values_to_rounded_decimal : Specification
{
    /// <summary>Expected values were generated with (decimal)double and (decimal)float on .NET 10.</summary>
    const double TwoToThe96 = 79228162514264337593543950336d;

    Exception _twoToThe96;
    Exception _twoToThe96AsSingle;

    void Because()
    {
        _twoToThe96 = Catch.Exception(() => DecimalValues.FromDoubleRounded(TwoToThe96));
        _twoToThe96AsSingle = Catch.Exception(() => DecimalValues.FromSingleRounded(7.9228163E+28f));
    }

    [Fact] void should_round_half_way_digits_the_way_net_10_does() => decimal.GetBits(DecimalValues.FromDoubleRounded(68660835767.472549d)).ShouldEqual([2000834278, 159863, 0, 262144]);
    [Fact] void should_convert_the_largest_double_below_two_to_the_96() => decimal.GetBits(DecimalValues.FromDoubleRounded(Math.BitDecrement(TwoToThe96))).ShouldEqual([304791552, -8753, -1, 0]);
    [Fact] void should_convert_the_largest_float_below_two_to_the_96() => decimal.GetBits(DecimalValues.FromSingleRounded(MathF.BitDecrement(7.9228163E+28f))).ShouldEqual([536870912, -1282231782, -137, 0]);
    [Fact] void should_convert_1e_minus_28() => decimal.GetBits(DecimalValues.FromDoubleRounded(1e-28d)).ShouldEqual([1, 0, 0, 1835008]);
    [Fact] void should_convert_half_of_1e_minus_28_to_zero() => decimal.GetBits(DecimalValues.FromDoubleRounded(5e-29d)).ShouldEqual([0, 0, 0, 0]);
    [Fact] void should_convert_1_5e_minus_28_to_2e_minus_28() => decimal.GetBits(DecimalValues.FromDoubleRounded(1.5e-28d)).ShouldEqual([2, 0, 0, 1835008]);
    [Fact] void should_convert_single_1e_minus_28() => decimal.GetBits(DecimalValues.FromSingleRounded(1e-28f)).ShouldEqual([1, 0, 0, 1835008]);
    [Fact] void should_convert_double_epsilon_to_zero() => decimal.GetBits(DecimalValues.FromDoubleRounded(double.Epsilon)).ShouldEqual([0, 0, 0, 0]);
    [Fact] void should_throw_overflow_for_two_to_the_96() => _twoToThe96.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_single_two_to_the_96() => _twoToThe96AsSingle.ShouldBeOfExactType<OverflowException>();
    [Fact] void should_throw_overflow_for_negative_infinity() => Catch.Exception(() => DecimalValues.FromDoubleRounded(double.NegativeInfinity)).ShouldBeOfExactType<OverflowException>();
}
