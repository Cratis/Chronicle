// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_only_a_child_composite_mapping : Specification
{
    ReadModelScenario<CompositeSeedGroup> _scenario;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<CompositeSeedGroup>().WithProjection(builder => builder
        .Children(model => model.Children, children => children
            .IdentifiedBy(child => child.Id)
            .From<CompositeSeedRenamed>(from => from
                .UsingCompositeKey<CompositeSeedKey>(key => key
                    .Set(part => part.TenantId).To(@event => @event.TenantId)
                    .Set(part => part.Amount).To(@event => @event.Amount))
                .UsingParentKeyFromContext(context => context.EventSourceId))));

    void Because() => _error = Catch.Exception(() => _scenario.CollectReadModelFor(
        new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m }, new([])));

    void Destroy() => _scenario.Dispose();

    [Fact] void should_require_a_root_composite_mapping() => _error.ShouldBeOfExactType<InvalidReadModelSeedKey>();
}
