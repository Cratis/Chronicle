// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402, SA1649

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels;

namespace Cratis.Chronicle.Testing.for_3434_repros.issue_4266;

/// <summary>
/// Root From plus FromAll (FluentAllEventsProjection); an event on an unrelated event source must not create an
/// instance there (MongoDB-backed kernel returned null for that key per #4266).
/// </summary>
public class when_an_unrelated_event_source_only_has_a_from_all_event : Specification
{
    ReadModelScenario<FluentAllEvents> _scenario;
    EventSourceId _thing;
    EventSourceId _unrelated;

    void Establish()
    {
        _scenario = new ReadModelScenario<FluentAllEvents>();
        _thing = EventSourceId.New();
        _unrelated = EventSourceId.New();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_thing).Events(new FluentThingOpened("Acme"));
        await _scenario.Given.ForEventSource(_unrelated).Events(new FluentThingTouched());
    }

    [Fact] void should_not_create_an_instance_for_the_unrelated_source() => _scenario.InstanceForEventSourceId(_unrelated).ShouldBeNull();
    [Fact] void should_only_have_one_instance() => _scenario.Instances.Count.ShouldEqual(1);
    [Fact] void should_count_only_the_things_own_event() => _scenario.InstanceForEventSourceId(_thing)!.EventCount.ShouldEqual(1);
}

public class when_only_an_unrelated_from_all_event_is_seeded : Specification
{
    ReadModelScenario<FluentAllEvents> _scenario;

    void Establish() => _scenario = new ReadModelScenario<FluentAllEvents>();

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new FluentThingTouched());

    [Fact] void should_not_materialize_an_instance() => _scenario.Instance.ShouldBeNull();
}
