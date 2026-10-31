// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_confirming_bulk_writes;

public class and_the_reducer_has_a_window : given.a_reducer_replay_handler
{
    readonly BulkWindowId _window = new(Guid.NewGuid());
    BulkFlushResult _result;

    void Establish() => _sink.FlushBulk().Returns(new BulkFlushResult(_window, []));

    async Task Because() => _result = await ((ICanHandleReplayForObserver)_handler).FlushFor(_observerDetails);

    [Fact] void should_flush_the_cached_sink() => _sink.Received(1).FlushBulk();
    [Fact] void should_return_the_window_id() => _result.WindowId.ShouldEqual(_window);
    [Fact] void should_not_close_the_window() => _sink.DidNotReceive().EndBulk();
}
