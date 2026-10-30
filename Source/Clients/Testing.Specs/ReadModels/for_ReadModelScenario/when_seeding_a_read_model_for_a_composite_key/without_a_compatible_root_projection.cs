// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class without_a_compatible_root_projection : Specification
{
    readonly CompositeSeedModel _seed = new(new("tenant", 1m), "seed", "retained", 5);
    readonly Dictionary<string, object?> _parts = new() { ["tenantId"] = "tenant", ["amount"] = 1m };
    Exception _missing;
    Exception _scalar;
    Exception _incompatible;
    Exception _reducer;
    Exception _lookup;

    void Because()
    {
        using var missing = new ReadModelScenario<CompositeSeedModel>();
        _missing = Catch.Exception(() => missing.CollectReadModelFor(_parts, _seed));
        using var scalar = new ReadModelScenario<CompositeSeedModel>().WithProjection(builder => builder
            .From<CompositeSeedRenamed>(from => from.UsingKey(@event => @event.TenantId)));
        _scalar = Catch.Exception(() => scalar.CollectReadModelFor(_parts, _seed));
        using var incompatible = new ReadModelScenario<CompositeSeedModel>().WithProjection(builder =>
        {
            builder.From<CompositeSeedRenamed>(from => from.UsingCompositeKey<CompositeSeedKey>(key => key
                .Set(part => part.TenantId).To(@event => @event.TenantId)
                .Set(part => part.Amount).To(@event => @event.Amount)));
            builder.From<CompositeSeedChanged>(from => from.UsingCompositeKey<CompositeSeedKey>(key => key
                .Set(part => part.TenantId).To(@event => @event.TenantId)));
        });
        _incompatible = Catch.Exception(() => incompatible.CollectReadModelFor(_parts, _seed));
        using var reducer = new ReadModelScenario<SeededJsonTally>();
        _reducer = Catch.Exception(() => reducer.CollectReadModelFor(_parts, new(Guid.Empty, 5, [])));
        using var lookup = new ReadModelScenario<LookupOnlySeed>();
        _lookup = Catch.Exception(() => lookup.CollectReadModelFor(_parts, new("lookup")));
    }

    [Fact] void should_refuse_missing_projection() => _missing.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_scalar_root() => _scalar.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_incompatible_root_sets() => _incompatible.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_reducer() => _reducer.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_refuse_lookup_only() => _lookup.ShouldBeOfExactType<InvalidReadModelSeedKey>();
}
