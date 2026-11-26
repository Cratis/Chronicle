// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry.when_revising_a_definition;

public class and_its_content_is_unchanged : given.a_registry
{
    ObserverDefinition _stored;

    void Establish() => _storage.Seed(DefinitionOf(_observerId, _firstEventType) with { Revision = 3 });

    async Task Because()
    {
        await _registry.Save(DefinitionOf(_observerId, _firstEventType));
        _stored = await _storage.Get(_observerId);
    }

    [Fact] void should_keep_the_stored_revision() => _stored.Revision.ShouldEqual(new ObserverDefinitionRevision(3));
}
