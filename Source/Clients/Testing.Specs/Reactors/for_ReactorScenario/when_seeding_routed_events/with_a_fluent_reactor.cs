// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.given;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_seeding_routed_events;

public class with_a_fluent_reactor : Specification
{
    ReactorSourceGivenBuilder _original;
    ReactorSourceGivenBuilder _routed;
    readonly List<EventContext> _contexts = [];
    Defaults _defaults;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider { EventTypes = [typeof(ReactorRouteRecorded)] };
        var options = new ChronicleOptions();
        options.ExplicitArtifacts.RegisterReactor("routes", builder => builder.On<ReactorRouteRecorded>((@event, context) => _contexts.Add(context)));
        _defaults = new(artifacts, null, options);
        var scenario = new ReactorScenario("routes", _defaults);
        _original = scenario.Given.ForEventSource("source");
    }

    async Task Because()
    {
        _routed = _original.OnStream(eventSourceType: "RouteOrder", eventStreamType: "Payments", eventStreamId: "monthly");
        await _routed.Events(new ReactorRouteRecorded("routed"));
        await _original.Events(new ReactorRouteRecorded("default"));
    }

    void Destroy() => ((IDisposable)_defaults.EventStore).Dispose();

    [Fact] void should_deliver_the_route_to_the_callback() => _contexts[0].EventSourceType.ShouldEqual(new EventSourceType("RouteOrder"));
    [Fact] void should_deliver_the_stream_type_to_the_callback() => _contexts[0].EventStreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_deliver_the_stream_id_to_the_callback() => _contexts[0].EventStreamId.ShouldEqual(new EventStreamId("monthly"));
    [Fact] void should_leave_the_original_source_type_unchanged() => _contexts[^1].EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_leave_the_original_stream_type_unchanged() => _contexts[^1].EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_leave_the_original_stream_id_unchanged() => _contexts[^1].EventStreamId.ShouldEqual(new EventStreamId(EventStreamId.Default));
    [Fact] void should_create_a_separate_builder() => ReferenceEquals(_original, _routed).ShouldBeFalse();
}
