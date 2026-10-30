// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class when_replacing_projection : a_composite_projection
{
    readonly Dictionary<string, object?> _parts = new() { ["tenantId"] = "tenant", ["amount"] = 1m };
    Exception _error;
    CompositeSeedModel _result;

    async Task Because()
    {
        await _scenario.Given.ForKey(_parts).ReadModel(new(new("tenant", 1m), "seed", "retained", 5));
        _ = _scenario.Instance;
        _error = Catch.Exception(() => _scenario.WithProjection(builder => { }));
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("tenant", 1m, "after"));
        _result = _scenario.InstanceForKey(_parts)!;
    }

    [Fact] void should_refuse_projection_change() => _error.ShouldBeOfExactType<ReadModelSeedProjectionCannotChange>();
    [Fact] void should_keep_original_projection() => _result.Count.ShouldEqual(6);
    [Fact] void should_keep_seed_state() => _result.Untouched.ShouldEqual("retained");
}
