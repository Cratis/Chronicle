// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_composite_key;

public class with_seeded_native_scalar_formats : a_native_projection
{
    NativeSeedModel _result;
    string _nativeIdentity;

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("source").Events(Event());
        _nativeIdentity = _scenario.Instances.Keys.Single().Value;
        await _scenario.Given.ForKey(_parts).ReadModel(new(_key, 10));
        _result = _scenario.InstanceForKey(_parts)!;
    }

    [Fact] void should_update_seed_with_native_event() => _result.Count.ShouldEqual(11);
    [Fact] void should_keep_all_typed_parts() => _result.Id.ShouldEqual(_key);
    [Fact] void should_have_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
    [Fact] void should_have_native_sink_identity() => _scenario.Instances.Keys.Single().Value.ShouldEqual(_nativeIdentity);
}
