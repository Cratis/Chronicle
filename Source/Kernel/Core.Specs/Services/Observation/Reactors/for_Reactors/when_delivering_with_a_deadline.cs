// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_delivering_with_a_deadline : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _withDeadline = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _withoutDeadline = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _alreadyDue = new(TaskCreationOptions.RunContinuationsAsynchronously);

    void Because()
    {
        _deliver("partition", [], _withDeadline, TimeSpan.FromSeconds(30));
        _deliver("partition", [], _withoutDeadline);
        _deliver("partition", [], _alreadyDue, TimeSpan.Zero);
    }

    [Fact] void should_tell_the_client_how_long_the_kernel_waits() => _deliveries[0].DeliverWithinMilliseconds.ShouldEqual(30_000UL);
    [Fact] void should_send_zero_when_there_is_no_deadline() => _deliveries[1].DeliverWithinMilliseconds.ShouldEqual(0UL);
    [Fact] void should_never_send_zero_for_a_deadline_that_is_due() => _deliveries[2].DeliverWithinMilliseconds.ShouldEqual(1UL);
}
