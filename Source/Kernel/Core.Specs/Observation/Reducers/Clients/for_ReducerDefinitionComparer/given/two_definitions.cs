// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.given;

public class two_definitions : Specification
{
    protected ReducerDefinitionComparer _comparer;
    protected ReducerKey _key;
    protected ReducerDefinition _previous;
    protected ReducerDefinition _current;
    protected ReducerDefinitionCompareResult _result;

    void Establish()
    {
        _key = new("reducer", "store", "namespace", EventSequenceId.Log);
        var storage = Substitute.For<IStorage>();
        storage.GetEventStore(_key.EventStore).Reducers.Has(_key.ReducerId).Returns(true);
        _comparer = new(storage);
        _previous = new(
            "reducer",
            EventSequenceId.Log,
            [new(new EventType("event", 1), WellKnownExpressions.EventSourceId)],
            "read-model",
            true,
            ["first", "second"],
            new ObserverFilters(["one", "two"], "source", "stream"),
            "hash");
        _current = _previous with
        {
            Tags = ["second", "first"],
            Filters = new ObserverFilters(["two", "one"], "source", "stream"),
            EventTypes = [new(new EventType("event", 1), WellKnownExpressions.EventSourceId)]
        };
    }
}
