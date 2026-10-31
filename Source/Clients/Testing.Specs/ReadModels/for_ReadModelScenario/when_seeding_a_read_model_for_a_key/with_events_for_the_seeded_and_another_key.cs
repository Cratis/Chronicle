// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_events_for_the_seeded_and_another_key : Specification
{
    ReadModelScenario<SeededKeyModel> _scenario;
    IReadModels _readModels;
    SeededKeyModel _seeded;
    SeededKeyModel _other;
    SeededKeyModel _lookup;

    void Establish()
    {
        _scenario = new(new SeededKeyModel(null!, "initial", "baseline", 2));
        _scenario.Given.ForEventSourceId("invoice-1").ReadModel(new("explicit-identifier", "before", "retained", 5));
        _readModels = _scenario.ReadModels;
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId("invoice-1").Events(new SeededKeyRenamed("after"));
        await _scenario.Given.ForEventSourceId("invoice-2").Events(new SeededKeyRenamed("other"));
        _lookup = await _readModels.GetInstanceById<SeededKeyModel>(new EventSourceId("invoice-1"));
        _seeded = _scenario.InstanceForEventSourceId("invoice-1")!;
        _other = _scenario.InstanceForEventSourceId("invoice-2")!;
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_apply_setters_on_top_of_the_seed() => _seeded.Name.ShouldEqual("after");
    [Fact] void should_preserve_the_seeded_identifier() => _seeded.Id.ShouldEqual("explicit-identifier");
    [Fact] void should_preserve_untouched_seeded_properties() => _seeded.Untouched.ShouldEqual("retained");
    [Fact] void should_increment_the_seeded_value() => _seeded.Count.ShouldEqual(6);
    [Fact] void should_start_the_other_key_from_the_normal_initial_state() => _other.Count.ShouldEqual(3);
    [Fact] void should_not_leak_seeded_properties_to_the_other_key() => _other.Untouched.ShouldEqual("baseline");
    [Fact] void should_return_the_folded_instance_from_a_retained_read_models_surface() => _lookup.ShouldEqual(_seeded);
}
