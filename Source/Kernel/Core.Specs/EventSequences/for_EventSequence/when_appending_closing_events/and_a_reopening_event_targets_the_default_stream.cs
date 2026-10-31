// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_closing_events;

public class and_a_reopening_event_targets_the_default_stream : given.a_stream_only_closing_constraint
{
    AppendResult _result;

    void Establish() => _eventType = new("Reopened", EventTypeGeneration.First);

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_the_reopening_event() => _result.ConstraintViolations.ShouldNotBeEmpty();
    [Fact] void should_persist_nothing() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
