// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering_fluently;

public class and_delivering_an_event : given.an_observed_reactor_stream
{
    readonly TaskCompletionSource<(object Event, EventContext Context)> _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    IReactorHandler _handler;
    ReactorResult _outcome;
    (object Event, EventContext Context) _delivered;

    async Task Because()
    {
        _handler = await _reactors.Register("orders", reactor => reactor.On<OrderPlaced>((@event, context) =>
        {
            _received.TrySetResult((@event, context));
            return Task.CompletedTask;
        }));
        Deliver(_orderPlaced, "{\"orderNumber\":\"42\"}");
        _outcome = await _result.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _delivered = await _received.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_subscribe_the_observer_to_the_handled_event_type() => _definitions.Single().EventTypes.Single().EventType.Id.ShouldEqual(_orderPlaced.Id.Value);
    [Fact] void should_subscribe_to_the_handled_generation() => _definitions.Single().EventTypes.Single().EventType.Generation.ShouldEqual(_orderPlaced.Generation.Value);
    [Fact] void should_register_under_the_given_identifier() => _definitions.Single().ReactorId.ShouldEqual("orders");
    [Fact] void should_be_replayable() => _definitions.Single().IsReplayable.ShouldBeTrue();
    [Fact] void should_hand_the_deserialized_event_to_the_handler() => _delivered.Event.ShouldEqual(_event);
    [Fact] void should_hand_the_context_to_the_handler() => _delivered.Context.SequenceNumber.ShouldEqual((EventSequenceNumber)12UL);
    [Fact] void should_acknowledge_the_event() => _outcome.State.ShouldEqual(ObservationState.Success);
    [Fact] void should_advance_past_the_event() => _outcome.LastSuccessfulObservation.ShouldEqual(12UL);
    [Fact] void should_be_reachable_by_its_identifier() => _reactors.GetHandlerById("orders").ShouldEqual(_handler);
    [Fact] void should_report_object_as_the_reactor_type() => _handler.ReactorType.ShouldEqual(typeof(object));
}
