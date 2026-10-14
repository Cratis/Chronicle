// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_the_original_context : a_recorded_occurrence
{
    async Task Because() => await _scenario.Redeliver(_occurrence);

    [Fact] void should_invoke_the_handler_once() => _occurrenceRecorder.Received(1).Record(_event, _occurrence.Context, Arg.Any<ReactorDelivery>());
    [Fact] void should_reuse_the_context_instance() => _occurrenceRecorder.Received().Record(_event, Arg.Is<EventContext>(context => ReferenceEquals(context, _occurrence.Context)), Arg.Any<ReactorDelivery>());
    [Fact] void should_preserve_the_sequence_number() => _occurrenceRecorder.Received().Record(_event, Arg.Is<EventContext>(context => context.SequenceNumber == _occurrence.Context.SequenceNumber), Arg.Any<ReactorDelivery>());
    [Fact] void should_preserve_the_correlation() => _occurrenceRecorder.Received().Record(_event, Arg.Is<EventContext>(context => context.CorrelationId == _occurrence.Context.CorrelationId), Arg.Any<ReactorDelivery>());
    [Fact] void should_preserve_the_occurrence_time() => _occurrenceRecorder.Received().Record(_event, Arg.Is<EventContext>(context => context.Occurred == _occurrence.Context.Occurred), Arg.Any<ReactorDelivery>());
    [Fact] void should_preserve_the_route() => _occurrenceRecorder.Received().Record(_event, Arg.Is<EventContext>(context => context.EventSourceType == new EventSourceType("RouteOrder") && context.EventStreamType == new EventStreamType("Payments") && context.EventStreamId == new EventStreamId("monthly")), Arg.Any<ReactorDelivery>());
    [Fact] void should_derive_the_delivery_from_the_recorded_context() => _occurrenceRecorder.Received().Record(_event, _occurrence.Context, ReactorDelivery.For(new OccurrenceRecordingReactor(_occurrenceRecorder), _occurrence.Context));
}
