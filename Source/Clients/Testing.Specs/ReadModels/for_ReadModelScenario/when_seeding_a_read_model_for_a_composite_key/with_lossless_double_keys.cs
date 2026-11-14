// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_lossless_double_keys : a_double_projection
{
    readonly List<Exception?> _errors = [];
    Dictionary<double, DoubleSeedModel> _results;

    void Because()
    {
        object[] invalid = [double.NaN, double.PositiveInfinity, double.NegativeInfinity, float.NaN, float.PositiveInfinity, float.NegativeInfinity, 9007199254740993L, long.MaxValue];
        foreach (var value in invalid)
        {
            _errors.Add(Catch.Exception(() => _scenario.CollectReadModelFor(
                new Dictionary<string, object?> { ["amount"] = value }, new(new(0), 10))));
        }
        (object Value, double Expected)[] valid =
        [
            (1.5d, 1.5d), (1e30d, 1e30d), (double.MaxValue, double.MaxValue),
            (9007199254740992L, 9007199254740992d), (0.5f, 0.5d)
        ];
        foreach (var (value, expected) in valid)
        {
            _scenario.CollectReadModelFor(new Dictionary<string, object?> { ["amount"] = value }, new(new(expected), 10));
        }
        _results = _scenario.Instances.Values.ToDictionary(model => model.Id.Amount);
    }

    [Fact] void should_refuse_double_nan() => _errors[0].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_double_positive_infinity() => _errors[1].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_double_negative_infinity() => _errors[2].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_float_nan() => _errors[3].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_float_positive_infinity() => _errors[4].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_float_negative_infinity() => _errors[5].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_an_integer_that_rounds_in_double() => _errors[6].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_long_max_value_that_rounds_in_double() => _errors[7].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_preserve_a_fractional_double() => _results[1.5d].Id.Amount.ShouldEqual(1.5d);
    [Fact] void should_preserve_a_double_above_the_decimal_range() => _results[1e30d].Id.Amount.ShouldEqual(1e30d);
    [Fact] void should_preserve_the_maximum_finite_double() => _results[double.MaxValue].Id.Amount.ShouldEqual(double.MaxValue);
    [Fact] void should_preserve_an_exact_integer_as_a_double() => _results[9007199254740992d].Id.Amount.ShouldEqual(9007199254740992d);
    [Fact] void should_preserve_a_float_widened_to_double() => _results[0.5d].Id.Amount.ShouldEqual(0.5d);
    [Fact] void should_preserve_the_seeded_state() => _results[1e30d].Count.ShouldEqual(10);
    [Fact] void should_register_only_the_valid_seeds() => _results.Count.ShouldEqual(5);
}
