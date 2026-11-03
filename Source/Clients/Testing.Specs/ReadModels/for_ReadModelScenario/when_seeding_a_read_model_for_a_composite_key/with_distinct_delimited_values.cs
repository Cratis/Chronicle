// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_distinct_delimited_values : a_composite_projection
{
    readonly Dictionary<string, object?> _first = new() { ["tenantId"] = "tenant_amount_1:ck:", ["amount"] = 2m };
    readonly Dictionary<string, object?> _second = new() { ["amount"] = 1m, ["tenantId"] = "tenant_amount_2:ck:" };
    CompositeSeedModel _firstResult;
    CompositeSeedModel _secondResult;
    CompositeSeedModel? _unregistered;

    async Task Because()
    {
        await _scenario.Given.ForKey(_first).ReadModel(new(new("tenant_amount_1:ck:", 2m), "first", "first", 10));
        await _scenario.Given.ForKey(_second).ReadModel(new(new("tenant_amount_2:ck:", 1m), "second", "second", 20));
        await _scenario.Given.ForEventSourceId("unrelated").Events(new CompositeSeedRenamed("tenant_amount_1:ck:", 2m, "changed"), new CompositeSeedRenamed("unregistered", 3m, "third"));
        _firstResult = _scenario.InstanceForKey(_first)!;
        _secondResult = _scenario.InstanceForKey(_second)!;
        _unregistered = _scenario.InstanceForKey(new Dictionary<string, object?> { ["tenantId"] = "unregistered", ["amount"] = 3m });
    }

    [Fact] void should_keep_distinct_seeded_identities() => _scenario.Instances.Count.ShouldEqual(3);
    [Fact] void should_update_only_first_seed() => _firstResult.Count.ShouldEqual(11);
    [Fact] void should_keep_second_seed() => _secondResult.Count.ShouldEqual(20);
    [Fact] void should_not_query_unregistered_composite_instances() => _unregistered.ShouldBeNull();
}
