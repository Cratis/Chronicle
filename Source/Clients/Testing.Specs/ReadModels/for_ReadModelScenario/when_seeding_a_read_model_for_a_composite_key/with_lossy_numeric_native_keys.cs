// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_lossy_numeric_native_keys : a_native_projection
{
    readonly List<Exception?> _errors = [];
    NativeSeedModel _result;

    void Because()
    {
        (string Part, object Value)[] invalid =
        [
            ("number", 1e-30d), ("number", 2.0000000000000004d),
            ("number", 1e-30f), ("number", 1.0000001f),
            ("number", 2.5d), ("number", -1e-30d),
            ("large", 1e30d), ("large", (double)long.MaxValue)
        ];
        foreach (var (part, value) in invalid)
        {
            var parts = new Dictionary<string, object?>(_parts) { [part] = value };
            _errors.Add(Catch.Exception(() => _scenario.CollectReadModelFor(parts, new(_key, 10))));
        }
        var valid = new Dictionary<string, object?>(_parts) { ["number"] = 2d, ["large"] = 9007199254740992d };
        _scenario.CollectReadModelFor(valid, new(_key with { Number = 2, Large = 9007199254740992L }, 10));
        _result = _scenario.Instance!;
    }

    [Fact] void should_refuse_a_tiny_fractional_double() => _errors[0].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_double_just_above_an_integer() => _errors[1].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_tiny_fractional_float() => _errors[2].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_float_just_above_an_integer() => _errors[3].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_fractional_double() => _errors[4].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_negative_tiny_fractional_double() => _errors[5].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_double_outside_the_long_range() => _errors[6].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_a_double_rounded_above_long_max_value() => _errors[7].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_accept_an_integral_double_for_an_integer() => _result.Id.Number.ShouldEqual(2);
    [Fact] void should_accept_an_exact_integral_double_for_a_long() => _result.Id.Large.ShouldEqual(9007199254740992L);
    [Fact] void should_register_only_the_valid_seed() => _scenario.Instances.Count.ShouldEqual(1);
}
