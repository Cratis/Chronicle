// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class without_events : Specification
{
    ReadModelScenario<SeededKeyModel> _scenario;
    readonly SeededKeyModel _seed = new("identifier-as-given", "before", "retained", 5);

    void Establish() => _scenario = new();
    async Task Because() => await _scenario.Given.ForEventSourceId("invoice-1").ReadModel(_seed);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_include_the_seeded_key_in_instances() => _scenario.Instances.Keys.Select(key => key.Value).ShouldContainOnly("invoice-1");
    [Fact] void should_return_the_seeded_instance_by_key() => _scenario.InstanceForEventSourceId("invoice-1").ShouldEqual(_seed);
    [Fact] void should_return_the_single_seeded_instance() => _scenario.Instance.ShouldEqual(_seed);
    [Fact] void should_not_materialize_an_unseeded_key() => _scenario.InstanceForEventSourceId("invoice-2").ShouldBeNull();
}
