// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_the_only_stream_for_a_connection_ends : given.two_observation_streams_for_the_same_connection
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    ObserverSubscriberState _state;

    async Task Because()
    {
        _earlierSubscription.Dispose();
        await _signals.Disconnected(0).WaitAsync(TimeSpan.FromSeconds(5));
        await _unsubscribed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Deliver(_result);
        _state = (await _result.Task.WaitAsync(TimeSpan.FromSeconds(5))).State;
    }

    [Fact] void should_report_the_connection_as_disconnected() => _state.ShouldEqual(ObserverSubscriberState.Disconnected);
    [Fact] void should_unsubscribe_the_reactor() => _reactor.Received(1).Unsubscribe();
}
