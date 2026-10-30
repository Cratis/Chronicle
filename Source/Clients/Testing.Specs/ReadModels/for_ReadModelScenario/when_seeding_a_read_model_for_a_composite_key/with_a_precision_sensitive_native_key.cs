// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_a_precision_sensitive_native_key : a_composite_projection
{
    const decimal Amount = 123456789.123456789123456789m;
    CompositeSeedModel _result;

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("event-source").Events(new CompositeSeedRenamed("tenant", Amount, "after"));
        _result = _scenario.Instance!;
    }

    [Fact] void should_preserve_the_native_decimal_key() => _result.Id.Amount.ShouldEqual(Amount);
    [Fact] void should_use_the_canonical_tenant_part() => _result.Id.TenantId.ShouldEqual("tenant");
    [Fact] void should_materialize_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
