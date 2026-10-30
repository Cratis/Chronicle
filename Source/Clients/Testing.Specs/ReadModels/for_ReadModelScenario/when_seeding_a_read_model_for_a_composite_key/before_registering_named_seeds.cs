// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class before_registering_named_seeds : Specification
{
    ReadModelScenario<CompositeSeedModel> _scenario;
    CompositeSeedModel _result;
    Exception _error;

    void Establish() => _scenario = new ReadModelScenario<CompositeSeedModel>().WithProjection(builder => builder
        .From<CompositeSeedRenamed>(from => from.UsingKey(@event => @event.TenantId)));

    async Task Because()
    {
        _scenario.WithProjection(builder => builder.From<CompositeSeedRenamed>(from => from
            .UsingCompositeKey<CompositeSeedKey>(key => key
                .Set(part => part.TenantId).To(@event => @event.TenantId)
                .Set(part => part.Amount).To(@event => @event.Amount))
            .Increment(model => model.Count)));
        var parts = new Dictionary<string, object?> { ["tenantId"] = "tenant", ["amount"] = 1m };
        await _scenario.Given.ForKey(parts).ReadModel(new(new("tenant", 1m), "seed", "retained", 5));
        _error = Catch.Exception(() => _scenario.WithProjection(builder => { }));
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("tenant", 1m, "after"));
        _result = _scenario.InstanceForKey(parts)!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_use_the_replacement_selected_before_seeding() => _result.Count.ShouldEqual(6);
    [Fact] void should_refuse_another_replacement_even_before_processing() => _error.ShouldBeOfExactType<ReadModelSeedProjectionCannotChange>();
    [Fact] void should_keep_the_seed() => _result.Untouched.ShouldEqual("retained");
}
