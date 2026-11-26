// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_the_safety_net_elapses : given.a_registry
{
    ObserverDefinition _before;
    ObserverDefinition _after;

    async Task Because()
    {
        _before = await _registry.Get(_observerId);
        _storage.Seed(DefinitionOf(_observerId, _secondEventType));
        _time.Advance(ObserverDefinitionRegistry.SafetyNetLifetime + TimeSpan.FromSeconds(1));
        _after = await _registry.Get(_observerId);
    }

    [Fact] void should_load_everything_again() => _storage.GetAllCalls.ShouldEqual(2);
    [Fact] void should_pick_up_the_change_that_was_never_announced() => _after.EventTypes.ShouldEqual([_secondEventType]);
}
