// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

public class a_composite_projection : Specification
{
    protected ReadModelScenario<CompositeSeedModel> _scenario;

    protected static Dictionary<string, object?> Parts(string tenant = "tenant", decimal amount = 1m) =>
        new() { ["tenantId"] = tenant, ["amount"] = amount };

    protected static void Define(IProjectionBuilderFor<CompositeSeedModel> builder) =>
        builder.From<CompositeSeedRenamed>(from => from
            .UsingCompositeKey<CompositeSeedKey>(key => key
                .Set(part => part.TenantId).To(@event => @event.TenantId)
                .Set(part => part.Amount).To(@event => @event.Amount))
            .Set(model => model.Name).To(@event => @event.Caption)
            .Increment(model => model.Count));

    void Establish() => _scenario = new ReadModelScenario<CompositeSeedModel>().WithProjection(Define);

    void Destroy() => _scenario.Dispose();
}

public record CompositeSeedKey(string TenantId, decimal Amount, string? Unmapped = null);

public record CompositeSeedModel(CompositeSeedKey Id, string Name, string Untouched, int Count);

[EventType]
public record CompositeSeedRenamed(string TenantId, decimal Amount, string Caption);
