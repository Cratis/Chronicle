// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexReplay.when_folding;

public class and_the_current_page_cannot_decode_an_event : given.a_replay
{
    void Establish()
    {
        Event(0, "healthy", "Claimed", "first");
        Event(1, "poisoned", "Claimed", "second");
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(Task.FromResult(true));
        cursor.Current.Returns(_ => BrokenPage());
        _sequence.Configure().GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>()).Returns(cursor);
        Start();
    }

    IEnumerable<AppendedEvent> BrokenPage()
    {
        yield return _events[0];
        throw new FormatException("bad second document");
    }

    async Task Because() => _result = await _replay.Continue();

    [Fact] void should_not_blame_the_last_successfully_decoded_event() => _result.Failed.Values.ShouldEachConformTo(failure => failure.Position == null);
    [Fact] void should_report_a_range_failure_until_the_bad_slot_is_located() => _replay.RangeReadFailed.ShouldBeTrue();
}
