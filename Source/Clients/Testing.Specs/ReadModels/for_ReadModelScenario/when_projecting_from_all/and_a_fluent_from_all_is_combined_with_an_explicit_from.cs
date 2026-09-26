// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_from_all;

public class and_a_fluent_from_all_is_combined_with_an_explicit_from : Specification
{
    ReadModelScenario<FluentAllEvents> _scenario;
    EventSourceId _id;

    void Establish()
    {
        _scenario = new ReadModelScenario<FluentAllEvents>().WithStrictEventSubscription();
        _id = new EventSourceId(Guid.NewGuid());
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_id)
            .Events(new FluentThingOpened("Acme"), new FluentThingTouched());

    [Fact] void should_retain_the_explicit_from_mapping() => _scenario.Instance!.Name.ShouldEqual("Acme");
    [Fact] void should_count_both_event_types_exactly_once() => _scenario.Instance!.EventCount.ShouldEqual(2);
}
