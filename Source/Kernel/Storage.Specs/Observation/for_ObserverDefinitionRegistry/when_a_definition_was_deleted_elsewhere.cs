// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry;

public class when_a_definition_was_deleted_elsewhere : given.a_registry
{
    bool _hasBeforeAnnouncement;
    bool _hasAfterAnnouncement;
    ObserverDefinition _after;

    async Task Because()
    {
        await _registry.Get(_observerId);
        _storage.Unseed(_observerId);
        _hasBeforeAnnouncement = await _registry.Has(_observerId);
        _registry.Invalidate(_observerId);
        _hasAfterAnnouncement = await _registry.Has(_observerId);
        _after = await _registry.Get(_observerId);
    }

    [Fact] void should_still_have_it_until_the_announcement_arrives() => _hasBeforeAnnouncement.ShouldBeTrue();
    [Fact] void should_not_have_it_after_the_announcement() => _hasAfterAnnouncement.ShouldBeFalse();
    [Fact] void should_not_bring_it_back() => _after.ShouldEqual(ObserverDefinition.Empty);
}
