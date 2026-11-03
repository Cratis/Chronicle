// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching_a_page;

public class and_a_partition_is_deferred : given.a_reducer_dispatcher
{
    void Establish() => _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>())
        .Returns(new ReduceResult(null, EventSequenceNumber.Unavailable, [], string.Empty) { RetryAfter = TimeSpan.FromSeconds(30) });

    async Task Because() => await _dispatcher.Dispatch(_handler, PageWith(("first", null, [1, 2])), "ns", Publish);

    [Fact] void should_defer_the_partition() => _published[0].Partitions[0].State.ShouldEqual(ObservationState.Deferred);
    [Fact] void should_ask_for_the_delay() => _published[0].Partitions[0].RetryAfterMilliseconds.ShouldEqual(30_000UL);
    [Fact] void should_send_no_read_model() => _published[0].Partitions[0].ReadModelState.ShouldBeNull();
}
