// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class without_events : a_composite_projection
{
    readonly Dictionary<string, object?> _parts = new() { ["tenantId"] = "a_:ck:1", ["amount"] = 42m };
    readonly CompositeSeedModel _seed = new(new("a_:ck:1", 42m), "seed", "retained", 5);
    CompositeSeedModel _result;

    async Task Because()
    {
        await _scenario.Given.ForKey(_parts).ReadModel(_seed);
        _parts["tenantId"] = "changed-after-registration";
        _result = _scenario.InstanceForKey(new Dictionary<string, object?> { ["amount"] = 42m, ["tenantId"] = "a_:ck:1" })!;
    }

    [Fact] void should_materialize_the_seed() => _result.ShouldEqual(_seed);
    [Fact] void should_have_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
    [Fact] void should_expose_the_primary() => _scenario.Instance.ShouldEqual(_seed);
}
