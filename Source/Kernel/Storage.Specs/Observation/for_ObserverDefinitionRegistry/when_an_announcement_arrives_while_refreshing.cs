// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_an_announcement_arrives_while_refreshing : given.a_registry
{
    ObserverDefinition _after;

    async Task Because()
    {
        await _registry.Get(_observerId);
        _storage.Seed(DefinitionOf(_observerId, _secondEventType));
        _registry.Invalidate(_observerId);
        _registry.Invalidate(_observerId);
        await _registry.Get(_observerId);
        _storage.Seed(DefinitionOf(_observerId, _firstEventType, _secondEventType));
        _registry.Invalidate(_observerId);
        _after = await _registry.Get(_observerId);
    }

    [Fact] void should_give_the_latest_definition() => _after.EventTypes.ShouldEqual([_firstEventType, _secondEventType]);
}
