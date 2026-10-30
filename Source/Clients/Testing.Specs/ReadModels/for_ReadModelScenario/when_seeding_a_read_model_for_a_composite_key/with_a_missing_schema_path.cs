// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_a_missing_schema_path : Specification
{
    ReadModelScenario<CompositeSeedModel> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<CompositeSeedModel>().WithProjection(builder => builder
        .From<CompositeSeedRenamed>(from => from.UsingCompositeKey<AcronymSeedKey>(key => key
            .Set(part => part.Revision).ToValue(1))));

    void Because() => _error = Catch.Exception(() => _scenario.CollectReadModelFor(
        new Dictionary<string, object?> { ["revision"] = 1 }, new(new("tenant", 1m), "seed", "retained", 5)));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_refuse_a_part_absent_from_the_identifier_schema() => _error.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_name_the_missing_path() => _error.Message.ShouldContain("id.revision");
}
