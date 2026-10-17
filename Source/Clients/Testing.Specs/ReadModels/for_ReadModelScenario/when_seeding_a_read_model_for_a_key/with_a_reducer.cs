// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_a_read_model_for_a_key;

public class with_a_reducer : Specification
{
    readonly EventSourceId _seededKey = EventSourceId.New();
    readonly EventSourceId _otherKey = EventSourceId.New();
    readonly EventSourceId _inactiveKey = EventSourceId.New();
    ReadModelScenario<SeededJsonTally> _scenario;
    SeededJsonTally _lookup;

    void Establish()
    {
        _scenario = new();
        _scenario.Given.ForEventSourceId(_seededKey).ReadModel(new(Guid.Parse(_seededKey.Value), 5, []));
        _scenario.Given.ForEventSourceId(_inactiveKey).ReadModel(new(Guid.Parse(_inactiveKey.Value), 8, []));
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSourceId(_seededKey).Events(new SeededJsonTallied());
        await _scenario.Given.ForEventSourceId(_otherKey).Events(new SeededJsonTallied());
        _lookup = await _scenario.ReadModels.GetInstanceById<SeededJsonTally>(_seededKey);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_use_the_seed_as_the_reducers_current_state() => _scenario.InstanceForEventSourceId(_seededKey)!.Count.ShouldEqual(6);
    [Fact] void should_reduce_the_other_key_independently() => _scenario.InstanceForEventSourceId(_otherKey)!.Count.ShouldEqual(1);
    [Fact] void should_retain_a_seed_with_no_events() => _scenario.InstanceForEventSourceId(_inactiveKey)!.Count.ShouldEqual(8);
    [Fact] void should_return_the_folded_instance_from_keyed_reads() => _lookup.Count.ShouldEqual(6);
}
