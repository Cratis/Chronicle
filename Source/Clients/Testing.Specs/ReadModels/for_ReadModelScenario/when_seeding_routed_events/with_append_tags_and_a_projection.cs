// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_append_tags_and_a_projection : Specification
{
    ReadModelScenario<ProjectedEventRoute> _scenario;
    readonly EventSourceId _source = EventSourceId.New();

    void Establish() => _scenario = new();

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).WithTags("first-tag").OnStream("Order", "Payments").Events(new RoutedEventRecorded("first"));
        await _scenario.Given.ForEventSource(_source).OnStream("Order", "Payments").WithTags("other-tag").Events(new RoutedEventRecorded("second"));
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_not_add_projection_tag_selection() => _scenario.Instance!.Value.ShouldEqual("second");
    [Fact] void should_process_both_original_positions() => _scenario.Instance!.Position.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_preserve_route_context() => _scenario.Instance!.StreamType.Value.ShouldEqual("Payments");
}
