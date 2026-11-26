// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation.for_ObserverDefinitionsStorage;

public class when_getting_definitions_for_event_types : given.an_observer_definitions_storage
{
    IEnumerable<ObserverDefinition> _forFirst;
    IEnumerable<ObserverDefinition> _replayableForSecond;

    async Task Because()
    {
        await _storage.Save(DefinitionObserving(_firstEventType));
        await _storage.Save(DefinitionObserving(_secondEventType) with { Identifier = "other-observer", IsReplayable = false });
        _forFirst = await _storage.GetForEventTypes([_firstEventType]);
        _replayableForSecond = await _storage.GetReplayableObserversForEventTypes([_secondEventType]);
    }

    [Fact] void should_find_the_observer_of_the_event_type() => _forFirst.Select(_ => _.Identifier).ShouldEqual([_observerId]);
    [Fact] void should_leave_out_observers_that_cannot_replay() => _replayableForSecond.ShouldBeEmpty();
}
