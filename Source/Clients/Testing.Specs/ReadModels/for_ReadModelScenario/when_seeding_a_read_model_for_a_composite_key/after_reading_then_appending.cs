// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class after_reading_then_appending : a_composite_projection
{
    readonly Dictionary<string, object?> _parts = new() { ["tenantId"] = "tenant", ["amount"] = 1m };
    CompositeSeedModel _first;
    CompositeSeedModel _second;

    async Task Because()
    {
        await _scenario.Given.ForKey(_parts).ReadModel(new(new("tenant", 1m), "seed", "retained", 5));
        _first = _scenario.Instance!;
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("tenant", 1m, "first"));
        _ = _scenario.InstanceForKey(_parts);
        await _scenario.Given.ForEventSourceId("source").Events(new CompositeSeedRenamed("tenant", 1m, "second"));
        _second = _scenario.InstanceForKey(_parts)!;
    }

    [Fact] void should_expose_seed_before_events() => _first.Count.ShouldEqual(5);
    [Fact] void should_fold_each_event_once() => _second.Count.ShouldEqual(7);
    [Fact] void should_keep_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
}
