// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class it_updates_the_constraint_index_with_the_appended_sequence_number : given.an_event_sequence
{
    Task Because() => _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);

    [Fact] void should_update_the_constraint_index_once() => _constraintIndexSequenceNumbers.Count.ShouldEqual(1);
    [Fact] void should_index_with_the_appended_sequence_number() => _constraintIndexSequenceNumbers[0].ShouldEqual(_appendedSequenceNumber);
}
