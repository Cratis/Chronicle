// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Reducers.Clients;

namespace Cratis.Chronicle.Services.Observation.Reducers.for_Reducers;

public class when_delivering_with_a_deadline : given.an_observation_stream
{
    readonly TaskCompletionSource<ReducerSubscriberResult> _withDeadline = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ReducerSubscriberResult> _withoutDeadline = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because()
    {
        _deliver(new("partition", [], null) { DeliverWithin = TimeSpan.FromMilliseconds(1500.2) }, _withDeadline);
        _deliver(new("partition", [], null), _withoutDeadline);
    }

    [Fact] void should_tell_the_client_how_long_the_kernel_waits() => _deliveries[0].DeliverWithinMilliseconds.ShouldEqual(1501UL);
    [Fact] void should_send_zero_when_there_is_no_deadline() => _deliveries[1].DeliverWithinMilliseconds.ShouldEqual(0UL);
}
