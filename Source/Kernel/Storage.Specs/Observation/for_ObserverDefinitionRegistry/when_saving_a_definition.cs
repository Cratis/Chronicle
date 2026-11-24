// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_saving_a_definition : given.a_registry
{
    ObserverDefinition _before;
    ObserverDefinition _other;
    ObserverDefinition _after;
    ObserverDefinition _otherAfter;

    async Task Because()
    {
        _before = await _registry.Get(_observerId);
        _other = await _registry.Get(_otherObserverId);
        _storage.ResetCounts();
        await _registry.Save(DefinitionOf(_observerId, _secondEventType));
        _after = await _registry.Get(_observerId);
        _otherAfter = await _registry.Get(_otherObserverId);
    }

    [Fact] void should_give_the_saved_definition() => _after.EventTypes.ShouldEqual([_secondEventType]);
    [Fact] void should_not_be_the_previous_instance() => ReferenceEquals(_before, _after).ShouldBeFalse();
    [Fact] void should_read_only_the_saved_definition_to_revise_and_refresh_it() => _storage.GetCalls.ShouldEqual(2);
    [Fact] void should_not_load_everything_again() => _storage.GetAllCalls.ShouldEqual(0);
    [Fact] void should_keep_the_instance_of_the_other_observer() => ReferenceEquals(_other, _otherAfter).ShouldBeTrue();
}
