// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_cursor_read_fails : given.a_replay
{
    Exception _error;

    void Establish()
    {
        Event(0, "source", "Claimed", "value");
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(Task.FromException<bool>(new IOException("cursor unavailable")));
        _sequence.Configure().GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        Start();
    }

    async Task Because() => _error = await Catch.Exception(async () => _result = await _replay.Continue());

    [Fact] void should_report_in_the_result_instead_of_throwing() => _error.ShouldBeNull();
    [Fact] void should_return_a_pending_result() => _result.IsCompleted.ShouldBeFalse();
    [Fact] void should_fail_all_folded_scopes() => _result.Failed.Select(name => name.Value).ShouldContainOnly("unique", "closing");
    [Fact] void should_not_invent_a_poison_event_position() => _result.FailingPosition.ShouldBeNull();
}
