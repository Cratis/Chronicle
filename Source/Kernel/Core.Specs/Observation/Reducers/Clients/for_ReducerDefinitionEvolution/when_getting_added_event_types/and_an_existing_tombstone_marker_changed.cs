// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionEvolution.when_getting_added_event_types;

public class and_an_existing_tombstone_marker_changed : Specification
{
    static readonly EventType _existing = new("existing", 1);
    static readonly EventType _added = new("added", 1, true);
    EventType[] _result;

    void Because() => _result = ReducerDefinitionEvolution.GetAddedEventTypesIfOnlyEventTypesChanged(
        Definition([_existing]), Definition([_existing with { Tombstone = true }, _added]));

    [Fact] void should_return_only_the_added_event_type() => _result.ShouldContainOnly(_added);
    [Fact] void should_preserve_the_added_types_marker() => _result.Single().Tombstone.ShouldBeTrue();

    static ReducerDefinition Definition(IEnumerable<EventType> types) =>
        new("reducer", EventSequenceId.Log, types.Select(_ => new EventTypeWithKeyExpression(_, WellKnownExpressions.EventSourceId)), "read-model", true, Tags: [], Hash: "hash");
}
