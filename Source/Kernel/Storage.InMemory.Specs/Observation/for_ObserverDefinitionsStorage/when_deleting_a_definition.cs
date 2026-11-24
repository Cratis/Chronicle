// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage;

public class when_deleting_a_definition : given.an_observer_definitions_storage
{
    bool _has;
    ObserverDefinition _stored;

    async Task Because()
    {
        await _storage.Save(DefinitionObserving(_firstEventType));
        await _storage.Delete(_observerId);
        _has = await _storage.Has(_observerId);
        _stored = await _storage.Get(_observerId);
    }

    [Fact] void should_no_longer_have_the_definition() => _has.ShouldBeFalse();
    [Fact] void should_give_the_empty_definition() => _stored.ShouldEqual(ObserverDefinition.Empty);
}
