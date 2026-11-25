// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_the_observer_is_unknown : given.a_registry
{
    ObserverDefinition _definition;
    bool _has;

    async Task Because()
    {
        _definition = await _registry.Get("not-an-observer");
        _has = await _registry.Has("not-an-observer");
    }

    [Fact] void should_give_what_the_storage_gives() => _definition.ShouldEqual(ObserverDefinition.Empty);
    [Fact] void should_not_have_it() => _has.ShouldBeFalse();
}
