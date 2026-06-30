// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_from_all;

/// <summary>
/// A read model with a <c>[FromAll]</c> property must update that property for EVERY driven event — both
/// the explicitly-subscribed one and the one observed only through <c>[FromAll]</c> — exactly like runtime.
/// </summary>
public class a_from_all_property_maps_a_context_property : Specification
{
    ReadModelScenario<ThingSummary> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<ThingSummary>();
        _id = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new ThingOpened("Acme"),
                new ThingTouched("poke"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_the_explicitly_subscribed_property() => _scenario.Instance!.Name.ShouldEqual("Acme");
    [Fact] void should_capture_opened_at_from_the_opening_event() => _scenario.Instance!.OpenedAt.ShouldNotEqual(default);
    [Fact] void should_populate_the_from_all_property() => _scenario.Instance!.LastUpdatedAt.ShouldNotBeNull();
    [Fact] void should_track_the_latest_event_in_the_from_all_property() => (_scenario.Instance!.LastUpdatedAt > _scenario.Instance!.OpenedAt).ShouldBeTrue();
}
