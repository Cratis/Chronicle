// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_Observers.when_acknowledging_a_revision;

public class and_the_silo_is_behind : given.an_observers_table
{
    ObserverDefinitionRevision _result;
    ObserverRoute _route;

    void Establish()
    {
        _stateProvider.Held = SnapshotWith(1, DefinitionAt(3));
        _stateProvider.Stored = SnapshotWith(2, DefinitionAt(4));
    }

    async Task Because()
    {
        _result = await _observers.Acknowledge(_eventStore, _observerId, 4);
        (await _observers.GetTable(_eventStore)).TryGetRoute(_observerId, out _route);
    }

    [Fact] void should_read_the_definition_again() => _stateProvider.Changes.ShouldEqual(1);
    [Fact] void should_answer_with_the_revision_it_now_holds() => _result.ShouldEqual(new ObserverDefinitionRevision(4));
    [Fact] void should_route_with_the_new_revision() => _route.Revision.ShouldEqual(new ObserverDefinitionRevision(4));
}
