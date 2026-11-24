// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry.when_revising_a_definition;

public class and_its_content_changed : given.a_registry
{
    ObserverDefinition _stored;
    ObserverDefinition _held;

    void Establish() => _storage.Seed(DefinitionOf(_observerId, _firstEventType) with { Revision = 3 });

    async Task Because()
    {
        await _registry.Save(DefinitionOf(_observerId, _secondEventType));
        _stored = await _storage.Get(_observerId);
        _held = await _registry.Get(_observerId);
    }

    [Fact] void should_store_the_next_revision() => _stored.Revision.ShouldEqual(new ObserverDefinitionRevision(4));
    [Fact] void should_hold_the_next_revision() => _held.Revision.ShouldEqual(new ObserverDefinitionRevision(4));
}
