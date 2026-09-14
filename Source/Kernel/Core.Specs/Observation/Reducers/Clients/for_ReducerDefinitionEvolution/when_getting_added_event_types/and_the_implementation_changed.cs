// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionEvolution.when_getting_added_event_types;

public class and_the_implementation_changed : Specification
{
    static readonly EventType _existingEventType = new("existing", 1);
    static readonly EventType _addedEventType = new("added", 1);
    EventType[] _result;

    void Because() => _result = ReducerDefinitionEvolution.GetAddedEventTypesIfOnlyEventTypesChanged(
        CreateDefinition([_existingEventType], "old-hash"),
        CreateDefinition([_existingEventType, _addedEventType], "new-hash"));

    [Fact] void should_not_claim_only_event_types_changed() => _result.ShouldBeEmpty();

    static ReducerDefinition CreateDefinition(IEnumerable<EventType> eventTypes, string hash) =>
        new("reducer", EventSequenceId.Log, eventTypes.Select(_ => new EventTypeWithKeyExpression(_, WellKnownExpressions.EventSourceId)), "read-model", true, Tags: [], Hash: hash);
}
