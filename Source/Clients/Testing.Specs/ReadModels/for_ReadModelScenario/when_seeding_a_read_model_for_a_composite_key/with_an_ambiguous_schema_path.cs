// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_an_ambiguous_schema_path : Specification
{
    ReadModelScenario<AmbiguousSeedModel> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<AmbiguousSeedModel>().WithProjection(builder => builder
        .From<CompositeSeedRenamed>(from => from.UsingCompositeKey<AmbiguousSeedKey>(key => key
            .Set(part => part.TenantId).To(@event => @event.TenantId))));

    void Because() => _error = Catch.Exception(() => _scenario.CollectReadModelFor(
        new Dictionary<string, object?> { ["tenantId"] = "tenant" }, new(new("tenant", "other"), 5)));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_refuse_case_ambiguous_schema_members() => _error.ShouldBeOfExactType<InvalidReadModelSeedKey>();
    [Fact] void should_name_the_ambiguous_path() => _error.Message.ShouldContain("id.tenantId");
}
