// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_acronym_property_names : Specification
{
    ReadModelScenario<AcronymSeedModel> _scenario;
    AcronymSeedModel _result;
    Exception _inventedNames;
    string _nativeIdentity;

    void Establish() => _scenario = new ReadModelScenario<AcronymSeedModel>().WithProjection(builder => builder
        .From<CompositeSeedRenamed>(from => from
            .UsingCompositeKey<AcronymSeedKey>(key => key
                .Set(part => part.URLId).To(@event => @event.TenantId)
                .Set(part => part.Revision).ToValue(1))
            .Increment(model => model.Count)));

    async Task Because()
    {
        var seed = new AcronymSeedModel(new("tenant", 1), 5);
        _inventedNames = Catch.Exception(() => _scenario.CollectReadModelFor(new Dictionary<string, object?> { ["urlId"] = "tenant", ["revision"] = 1 }, seed));
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("tenant", 1m, "after"));
        _nativeIdentity = _scenario.Instances.Keys.Single().Value;
        var parts = new Dictionary<string, object?> { ["URLId"] = "tenant", ["revision"] = 1 };
        _scenario.CollectReadModelFor(parts, seed);
        _result = _scenario.InstanceForKey(parts)!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_refuse_an_invented_camel_case_alias() => _inventedNames.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_update_seed_using_canonical_spelling() => _result.Count.ShouldEqual(6);
    [Fact] void should_share_the_native_identity() => _scenario.Instances.Keys.Single().Value.ShouldEqual(_nativeIdentity);
    [Fact] void should_keep_the_typed_identifier() => _result.Id.ShouldEqual(new("tenant", 1));
}
