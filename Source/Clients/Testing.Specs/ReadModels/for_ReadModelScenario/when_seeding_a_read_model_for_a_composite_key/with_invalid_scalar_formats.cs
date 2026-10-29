// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_invalid_scalar_formats : a_native_projection
{
    readonly List<Exception?> _errors = [];

    void Because()
    {
        (string Part, object Value)[] invalid =
        [
            ("identifier", "not-a-guid"), ("number", long.MaxValue), ("number", 1.5m),
            ("number", double.NaN), ("number", double.NegativeInfinity), ("enabled", "true"),
            ("day", "not-a-date"), ("occurred", "not-a-timestamp"), ("status", (SeedStatus)999),
            ("status", "UnknownValue"), ("code", new[] { "nested" })
        ];
        foreach (var (part, value) in invalid)
        {
            var parts = new Dictionary<string, object?>(_parts) { [part] = value };
            _errors.Add(Catch.Exception(() => _scenario.CollectReadModelFor(parts, new(_key, 10))));
        }
        _scenario.CollectReadModelFor(_parts, new(_key, 10));
    }

    [Fact] void should_refuse_invalid_guid() => _errors[0].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_integer_overflow() => _errors[1].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_fractional_integer() => _errors[2].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_nan() => _errors[3].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_infinity() => _errors[4].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_boolean_text() => _errors[5].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_invalid_date() => _errors[6].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_invalid_timestamp() => _errors[7].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_undefined_enum() => _errors[8].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_unknown_enum_name() => _errors[9].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_nested_values() => _errors[10].ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_register_only_valid_seed() => _scenario.Instances.Count.ShouldEqual(1);
}
