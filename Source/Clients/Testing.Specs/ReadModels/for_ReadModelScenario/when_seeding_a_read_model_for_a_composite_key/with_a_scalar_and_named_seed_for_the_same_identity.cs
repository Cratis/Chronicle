// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_a_scalar_and_named_seed_for_the_same_identity : a_composite_projection
{
    readonly CompositeSeedModel _seed = new(new("tenant", 1m), "first", "retained", 5);
    Exception _error;

    async Task Because()
    {
        await _scenario.Given.ForKey(new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m }).ReadModel(_seed);
        var identity = _scenario.Instances.Keys.Single();
        _error = Catch.Exception(() => _scenario.CollectReadModelFor(identity, _seed with { Name = "replacement" }));
    }

    [Fact] void should_refuse_the_conflicting_seed() => _error.ShouldBeOfExactType<ConflictingReadModelSeeds>();
    [Fact] void should_name_the_identity() => _error.Message.ShouldContain(_scenario.Instances.Keys.Single().Value);
    [Fact] void should_keep_named_seed() => _scenario.Instance.ShouldEqual(_seed);
}
