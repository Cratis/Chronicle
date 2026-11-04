// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.given;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_seeding_instances : a_borrowed_store
{
    readonly EventSourceId _seededKey = EventSourceId.New();
    readonly EventSourceId _inactiveKey = EventSourceId.New();
    ReadModelScenario<SeededJsonTally> _borrowed;
    ReadModelScenario<SeededJsonTally> _fresh;
    ReadModelScenario<SeededJsonTally> _other;
    SeededJsonTally _lookup;
    SeededJsonTally _otherLookup;
    SeededJsonTally _storeLookup;

    void Establish()
    {
        _borrowed = Borrowed<SeededJsonTally>();
        _fresh = Fresh<SeededJsonTally>();
        _other = Borrowed<SeededJsonTally>();
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            scenario.Given.ForEventSourceId(_seededKey).ReadModel(new(Guid.Parse(_seededKey.Value), 5, []));
            scenario.Given.ForEventSourceId(_inactiveKey).ReadModel(new(Guid.Parse(_inactiveKey.Value), 8, []));
        }
    }

    async Task Because()
    {
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            await scenario.Given.ForEventSourceId(_seededKey).Events(new SeededJsonTallied());
        }

        _lookup = await _borrowed.ReadModels.GetInstanceById<SeededJsonTally>(_seededKey);
        _otherLookup = await _other.ReadModels.GetInstanceById<SeededJsonTally>(_seededKey);
        _storeLookup = await _store.ReadModels.GetInstanceById<SeededJsonTally>(_seededKey);
    }

    [Fact] void should_fold_from_the_seed() => _borrowed.InstanceForEventSourceId(_seededKey)!.Count.ShouldEqual(6);
    [Fact] void should_retain_a_seed_with_no_events() => _borrowed.InstanceForEventSourceId(_inactiveKey)!.Count.ShouldEqual(8);
    [Fact] void should_return_the_folded_instance_from_its_read_models() => _lookup.Count.ShouldEqual(6);
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowed.Instances).ShouldEqual(Json(_fresh.Instances));
    [Fact] void should_not_share_seeds_with_another_scenario() => _otherLookup.ShouldBeNull();
    [Fact] void should_not_share_seeds_with_the_store() => _storeLookup.ShouldBeNull();
}
