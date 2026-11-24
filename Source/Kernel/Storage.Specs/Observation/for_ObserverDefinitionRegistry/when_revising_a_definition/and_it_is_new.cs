// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry.when_revising_a_definition;

public class and_it_is_new : given.a_registry
{
    static readonly Concepts.Observation.ObserverId _newObserverId = "a-new-observer";
    ObserverDefinition _stored;

    async Task Because()
    {
        await _registry.Save(DefinitionOf(_newObserverId, _firstEventType));
        _stored = await _storage.Get(_newObserverId);
    }

    [Fact] void should_store_the_first_revision() => _stored.Revision.ShouldEqual(new ObserverDefinitionRevision(1));
}
