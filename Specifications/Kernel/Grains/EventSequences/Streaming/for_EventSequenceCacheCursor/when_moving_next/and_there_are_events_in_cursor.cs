// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_there_are_events_in_cursor : given.cursor_with_ten_events_from_cache
{
    bool result;
    EventSequenceBatchContainer current;

    void Because()
    {
        result = cursor.MoveNext();
        current = (EventSequenceBatchContainer)cursor.GetCurrent(out var _);
    }

    [Fact] void should_move() => result.ShouldBeTrue();
    [Fact] void should_have_correct_current_event() => current.GetEvents<AppendedEvent>().Single().Item1.Metadata.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
}
