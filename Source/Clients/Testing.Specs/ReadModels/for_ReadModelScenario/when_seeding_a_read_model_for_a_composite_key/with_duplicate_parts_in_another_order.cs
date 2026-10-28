// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_duplicate_parts_in_another_order : a_composite_projection
{
    readonly CompositeSeedModel _seed = new(new("tenant", 1m), "first", "retained", 5);
    Exception _error;

    async Task Because()
    {
        await _scenario.Given.ForKey(new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m }).ReadModel(_seed);
        _error = Catch.Exception(() => _scenario.CollectReadModelFor(new Dictionary<string, object?> { ["amount"] = 1, ["tenantId"] = "tenant" }, _seed with { Name = "replacement" }));
    }

    [Fact] void should_refuse_canonical_duplicate() => _error.ShouldBeOfExactType<ReadModelAlreadySeeded>();
    [Fact] void should_keep_first_seed() => _scenario.Instance.ShouldEqual(_seed);
    [Fact] void should_register_only_once() => _scenario.Instances.Count.ShouldEqual(1);
}
