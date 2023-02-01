// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming.for_EventSequenceQueueCacheCursor;

public class when_getting_current_without_moving_next : given.an_event_sequence_queue_cache_cursor
{
    IBatchContainer result;
    Exception exception;

    void Because() => result = cursor.GetCurrent(out exception);

    [Fact] void should_throw_an_exception() => exception.ShouldNotBeNull();
    [Fact] void should_not_return_a_batch_container() => result.ShouldBeNull();
}
