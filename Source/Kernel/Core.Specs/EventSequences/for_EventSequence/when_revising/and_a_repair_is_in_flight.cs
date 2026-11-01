// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.InMemory.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_revising;

public class and_a_repair_is_in_flight : given.an_unfinished_constraint_repair
{
    readonly List<EventSequenceNumber> _starts = [];
    Exception? _error;

    async Task Establish()
    {
        _eventSequenceStorage.GetEventAt(Arg.Any<EventSequenceNumber>()).Returns(call => _log.GetEventAt(call.ArgAt<EventSequenceNumber>(0)));
        _eventSequenceStorage.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var start = call.ArgAt<EventSequenceNumber>(0);
                _starts.Add(start);
                return new EventCursor(_log.Events.Where(e => e.Context.SequenceNumber >= start && call.ArgAt<IEnumerable<EventType>>(5).Any(t => t.Id == e.Context.EventType.Id)).ToList());
            });
        await Task.CompletedTask;
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _eventSequence.Revise(0, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System));
        _clock.Advance(TimeSpan.FromSeconds(5));
        _eventSourceId = EventSourceId.New();
        _result = await AppendAnEvent();
    }

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_refold_from_the_revised_position() => _starts.ShouldContain(EventSequenceNumber.First);
}
