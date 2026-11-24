// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_deleting_a_definition : given.a_registry
{
    bool _has;
    ObserverDefinition _after;
    IEnumerable<ObserverDefinition> _all;

    async Task Because()
    {
        await _registry.Get(_observerId);
        await _registry.Delete(_observerId);
        _has = await _registry.Has(_observerId);
        _after = await _registry.Get(_observerId);
        _all = await _registry.GetAll();
    }

    [Fact] void should_no_longer_have_the_definition() => _has.ShouldBeFalse();
    [Fact] void should_give_the_empty_definition() => _after.ShouldEqual(ObserverDefinition.Empty);
    [Fact] void should_leave_the_other_observer() => _all.Select(_ => _.Identifier).ShouldEqual([_otherObserverId]);
}
