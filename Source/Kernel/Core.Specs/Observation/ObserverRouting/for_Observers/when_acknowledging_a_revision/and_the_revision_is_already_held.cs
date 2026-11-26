// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_Observers.when_acknowledging_a_revision;

public class and_the_revision_is_already_held : given.an_observers_table
{
    ObserverDefinitionRevision _result;

    void Establish() => _stateProvider.Held = SnapshotWith(1, DefinitionAt(3));

    async Task Because() => _result = await _observers.Acknowledge(_eventStore, _observerId, 3);

    [Fact] void should_answer_with_the_held_revision() => _result.ShouldEqual(new ObserverDefinitionRevision(3));
    [Fact] void should_not_read_the_definition_again() => _stateProvider.Changes.ShouldEqual(0);
}
