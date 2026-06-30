// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_from_all;

/// <summary>
/// A <c>[FromEvery]</c> property — the sibling of <c>[FromAll]</c> — must update for every driven event,
/// the same way the real runtime does.
/// </summary>
public class a_from_every_property_maps_a_context_property : Specification
{
    ReadModelScenario<ThingActivity> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<ThingActivity>();
        _id = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(
                new ThingOpened("Acme"),
                new ThingTouched("poke"));

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_populate_the_from_every_property() => _scenario.Instance!.LastActivityAt.ShouldNotBeNull();
}
