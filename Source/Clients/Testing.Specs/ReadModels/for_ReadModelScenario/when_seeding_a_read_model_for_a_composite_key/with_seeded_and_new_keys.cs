// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_seeded_and_new_keys : a_composite_projection
{
    readonly CompositeSeedModel _seed = new(new("tenant", 123456789.123456789m), "before", "retained", 10);
    readonly Dictionary<string, object?> _parts = new() { ["tenantId"] = "tenant", ["amount"] = 123456789.123456789m };
    CompositeSeedModel _result;
    CompositeSeedModel _other;

    async Task Establish() => await _scenario.Given.ForKey(_parts).ReadModel(_seed);

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("unrelated-source").Events(
            new CompositeSeedRenamed("tenant", 123456789.123456789m, "after"),
            new CompositeSeedRenamed("other", 1m, "other"));
        _result = _scenario.InstanceForKey(_parts)!;
        _other = _scenario.Instances.Values.Single(model => model.Id.TenantId == "other");
    }

    [Fact] void should_update_the_seed() => _result.Name.ShouldEqual("after");
    [Fact] void should_increment_from_seed() => _result.Count.ShouldEqual(11);
    [Fact] void should_preserve_untouched_state() => _result.Untouched.ShouldEqual("retained");
    [Fact] void should_preserve_identifier() => _result.Id.ShouldEqual(_seed.Id);
    [Fact] void should_start_other_key_normally() => _other.Count.ShouldEqual(1);
    [Fact] void should_have_exactly_two_instances() => _scenario.Instances.Count.ShouldEqual(2);
}
