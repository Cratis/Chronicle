// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Observation.for_ReducerReplayHandler.when_confirming_bulk_writes;

public class and_the_flush_fails : given.a_reducer_replay_handler
{
    readonly InvalidOperationException _failure = new("flush failed");
    Exception _error;

    void Establish() => _sink.FlushBulk().Returns(Task.FromException<BulkFlushResult>(_failure));

    async Task Because() => _error = await Catch.Exception(() => ((ICanHandleReplayForObserver)_handler).FlushFor(_observerDetails));

    [Fact] void should_not_confirm_the_window() => _error.ShouldEqual(_failure);
}
