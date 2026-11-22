// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage;

public class when_saving_a_definition_again_with_other_event_types : given.an_observer_definitions_storage
{
    ObserverDefinition _stored;
    IEnumerable<ObserverDefinition> _all;

    async Task Because()
    {
        await _storage.Save(DefinitionObserving(_firstEventType));
        await _storage.Save(DefinitionObserving(_secondEventType));
        _stored = await _storage.Get(_observerId);
        _all = await _storage.GetAll();
    }

    [Fact] void should_replace_the_event_types() => _stored.EventTypes.ShouldEqual([_secondEventType]);
    [Fact] void should_keep_one_definition_for_the_observer() => _all.Count().ShouldEqual(1);
}
