// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.V2.for_UniqueConstraintEncoder.when_encoding;

public class and_numbers_are_not_finite : Specification
{
    Exception[] _errors;

    void Because()
    {
        UniqueConstraintComponent[] inputs =
        [
            new(float.NaN, "float"), new(float.PositiveInfinity, "float"), new(float.NegativeInfinity, "float"),
            new(double.NaN, "double"), new(double.PositiveInfinity, "double"), new(double.NegativeInfinity, "double"),
            new(double.NaN), new(double.PositiveInfinity), new(double.NegativeInfinity),
            new(float.NaN, IsInSchema: false), new(double.PositiveInfinity, IsInSchema: false), new(double.NegativeInfinity, IsInSchema: false),
            new(new[] { float.NaN }, "float"), new(new[] { double.PositiveInfinity }, "double"), new(new[] { double.NegativeInfinity })
        ];
        _errors = inputs.Select(_ => Catch.Exception(() => { UniqueConstraintEncoder.Encode([1], [_]); })).ToArray();
    }

    [Fact] void should_reject_single_nan() => _errors[0].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_single_positive_infinity() => _errors[1].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_single_negative_infinity() => _errors[2].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_double_nan() => _errors[3].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_double_positive_infinity() => _errors[4].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_double_negative_infinity() => _errors[5].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_formatless_nan() => _errors[6].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_formatless_positive_infinity() => _errors[7].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_formatless_negative_infinity() => _errors[8].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_unknown_nan() => _errors[9].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_unknown_positive_infinity() => _errors[10].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_unknown_negative_infinity() => _errors[11].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_single_nan_array_leaves() => _errors[12].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_double_infinite_array_leaves() => _errors[13].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
    [Fact] void should_reject_formatless_infinite_array_leaves() => _errors[14].ShouldBeOfExactType<NonFiniteUniqueConstraintNumber>();
}
