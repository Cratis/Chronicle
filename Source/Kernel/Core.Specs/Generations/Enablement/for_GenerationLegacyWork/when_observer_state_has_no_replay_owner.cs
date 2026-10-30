// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationLegacyWork;

public class when_observer_state_has_no_replay_owner : given.legacy_work
{
    void Establish() => _observers = [new ObserverState(new("projection"), new(42), default, new HashSet<Key>(), new HashSet<Key>(), [], new(0), true, false)];

    async Task Because() => _blockers = await _reader.Inspect(new("orders"));

    [Fact] void should_keep_contradictory_evidence() => _blockers.Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.ContradictoryLegacyState);
    [Fact] void should_not_repair_observer_state() => _namespace.Observers.ReceivedCalls().All(call => call.GetMethodInfo().Name == "GetAll").ShouldBeTrue();
}
