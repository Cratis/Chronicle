// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

public class and_the_delegate_handles_two_events : given.a_reactor_dispatcher
{
    readonly List<ulong> _sequenceNumbers = [];

    async Task Because() => await _dispatcher.Dispatch(
        _handler,
        (@event, _) =>
        {
            _sequenceNumbers.Add(@event.Context.SequenceNumber.Value);
            return Task.CompletedTask;
        },
        EventsWith(5, 6),
        "ns",
        Publish,
        CancellationToken.None);

    [Fact] void should_call_the_delegate_in_event_order() => _sequenceNumbers.ShouldContainOnly(5UL, 6UL);
    [Fact] void should_publish_one_result() => _published.Count.ShouldEqual(1);
    [Fact] void should_succeed() => _published[0].State.ShouldEqual(ObservationState.Success);
    [Fact] void should_report_the_last_event_as_the_checkpoint() => _published[0].LastSuccessfulObservation.ShouldEqual(6UL);
}
