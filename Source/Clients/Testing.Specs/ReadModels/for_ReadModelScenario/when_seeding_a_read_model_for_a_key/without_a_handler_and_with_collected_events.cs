// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class without_a_handler_and_with_collected_events : Specification
{
    ReadModelScenario<LookupOnlySeed> _scenario;
    IReadModels _readModels;
    readonly LookupOnlySeed _seed = new("lookup only");
    LookupOnlySeed _result;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSourceId("invoice-1").ReadModel(_seed);
        _readModels = _scenario.ReadModels;
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("invoice-1").Events(new SeededKeyRenamed("after"));
        _result = await _readModels.GetInstanceById<LookupOnlySeed>("invoice-1");
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_return_the_registered_seed_without_processing_events() => _result.ShouldEqual(_seed);
    [Fact] void should_not_materialize_folded_instances() => _scenario.Instances.Count.ShouldEqual(0);
    [Fact] void should_not_materialize_a_primary_instance() => _scenario.Instance.ShouldBeNull();
}
