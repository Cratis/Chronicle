// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor.when_moving_next;

public class and_there_are_no_events : given.an_empty_cursor_and_empty_cache
{
    bool result;

    void Because() => result = cursor.MoveNext();

    [Fact] void should_not_move() => result.ShouldBeFalse();
}
