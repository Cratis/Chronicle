// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_appending_content_prepared_by_another_sequence : given.an_event_sequence
{
    PreparedEvent _prepared;
    Exception _error;

    void Establish() => _prepared = new(Substitute.For<IEventSequence>(), "event", new EventType("event", 1), "{}", null, []);

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.AppendPrepared("source", _prepared));

    [Fact] void should_reject_content_from_another_sequence_instance() => _error.ShouldBeOfExactType<PreparedEventBelongsToAnotherSequence>();
}
