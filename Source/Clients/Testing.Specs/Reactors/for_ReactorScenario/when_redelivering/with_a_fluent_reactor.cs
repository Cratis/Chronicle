// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_redelivering;

public class with_a_fluent_reactor : Specification
{
    ReactorScenario _scenario;
    Defaults _defaults;
    RecordedOccurrence _occurrence;
    IReadOnlyList<RecordedOccurrence> _several;
    readonly List<EventContext> _contexts = [];
    int _callbacksAfterHistory;

    async Task Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ReactorRouteRecorded), typeof(ProducedRouteRecorded)] };
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterReactor("recorded", builder => builder.On<ReactorRouteRecorded>((@event, context) => _contexts.Add(context)));
        _defaults = new(artifacts, null, options);
        _scenario = new("recorded", _defaults);
        _occurrence = await _scenario.Given.ForEventSource("source")
            .OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly")
            .History(new ReactorRouteRecorded("original"));
        _several = await _scenario.Given.ForEventSource("source").History(new ReactorRouteRecorded("second"), new ProducedRouteRecorded("unhandled"));
        _callbacksAfterHistory = _contexts.Count;
    }

    async Task Because()
    {
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_occurrence);
        await _scenario.Redeliver(_several[1]);
        await _scenario.Given.ForEventSource("source").Events(new ReactorRouteRecorded("live"));
    }

    void Destroy() => ((IDisposable)_defaults.EventStore).Dispose();

    [Fact] void should_not_invoke_callbacks_while_recording_history() => _callbacksAfterHistory.ShouldEqual(0);
    [Fact] void should_deliver_the_recorded_context_instance() => ReferenceEquals(_contexts[0], _occurrence.Context).ShouldBeTrue();
    [Fact] void should_reuse_the_context_for_each_redelivery() => _contexts[1].ShouldEqual(_contexts[0]);
    [Fact] void should_preserve_stream_routing() => _contexts[0].EventStreamId.ShouldEqual(new EventStreamId("monthly"));
    [Fact] void should_not_invoke_a_typed_callback_for_an_unhandled_event() => _contexts.Count.ShouldEqual(3);
    [Fact] void should_number_history_and_live_givens_contiguously() => _contexts[^1].SequenceNumber.ShouldEqual(EventSequenceNumber.First + 3);
    [Fact] void should_record_multiple_occurrences() => _several.Count.ShouldEqual(2);
}
