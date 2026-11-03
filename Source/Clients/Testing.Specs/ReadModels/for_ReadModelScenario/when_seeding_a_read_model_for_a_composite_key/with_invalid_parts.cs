// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_invalid_parts : a_composite_projection
{
    readonly CompositeSeedModel _seed = new(new("tenant", 1m), "seed", "retained", 5);
    readonly List<Exception?> _errors = [];

    void Because()
    {
        IReadOnlyDictionary<string, object?>[] invalid =
        [
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["tenantId"] = "tenant" },
            new Dictionary<string, object?> { ["TenantId"] = "tenant", ["amount"] = 1m },
            new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m, ["unmapped"] = "extra" },
            new Dictionary<string, object?> { ["tenantId"] = null, ["amount"] = 1m },
            new Dictionary<string, object?> { ["tenantId"] = new { Nested = "value" }, ["amount"] = 1m },
            new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = "invalid" },
            new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = double.PositiveInfinity }
        ];
        foreach (var parts in invalid)
        {
            _errors.Add(Catch.Exception(() => _scenario.CollectReadModelFor(parts, _seed)));
        }
        _scenario.CollectReadModelFor(new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m }, _seed);
    }

    [Fact] void should_reject_every_invalid_call() => _errors.TrueForAll(error => error is InvalidReadModelSeedKey).ShouldBeTrue();
    [Fact] void should_name_the_model() => _errors.TrueForAll(error => error!.Message.Contains(nameof(CompositeSeedModel), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_leave_no_partial_seeds() => _scenario.Instances.Count.ShouldEqual(1);
}
