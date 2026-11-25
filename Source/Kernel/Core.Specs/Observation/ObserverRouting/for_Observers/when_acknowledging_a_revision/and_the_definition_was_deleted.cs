// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.ObserverRouting.for_Observers.when_acknowledging_a_revision;

public class and_the_definition_was_deleted : given.an_observers_table
{
    ObserverDefinitionRevision _result;
    bool _routed;

    void Establish()
    {
        _stateProvider.Held = SnapshotWith(1, DefinitionAt(3));
        _stateProvider.Stored = SnapshotWith(2);
    }

    async Task Because()
    {
        _result = await _observers.Acknowledge(_eventStore, _observerId, ObserverDefinitionRevision.None);
        _routed = (await _observers.GetTable(_eventStore)).TryGetRoute(_observerId, out _);
    }

    [Fact] void should_read_the_definition_again() => _stateProvider.Changes.ShouldEqual(1);
    [Fact] void should_answer_that_it_holds_none() => _result.ShouldEqual(ObserverDefinitionRevision.None);
    [Fact] void should_no_longer_route_to_the_observer() => _routed.ShouldBeFalse();
}
