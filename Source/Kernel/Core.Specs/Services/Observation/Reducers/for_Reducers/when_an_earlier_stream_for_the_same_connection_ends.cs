// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_an_earlier_stream_for_the_same_connection_ends : given.two_observation_streams_for_the_same_connection
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish() => await OpenLaterStream();

    async Task Because()
    {
        _earlierSubscription.Dispose();
        await _signals.Disconnected(0).WaitAsync(TimeSpan.FromSeconds(5));
        Deliver(_result);
    }

    [Fact] void should_deliver_to_the_later_stream() => _laterDeliveries.Count.ShouldEqual(1);
    [Fact] void should_not_report_the_later_stream_as_disconnected() => _result.Task.IsCompleted.ShouldBeFalse();
    [Fact]
    async Task should_not_unsubscribe_the_reducer()
    {
        await _logger.StreamReplaced.WaitAsync(TimeSpan.FromSeconds(5));
        await _reducer.DidNotReceive().Unsubscribe();
    }

    [Fact] void should_not_deliver_to_the_earlier_stream() => _earlierDeliveries.Count.ShouldEqual(0);
}
