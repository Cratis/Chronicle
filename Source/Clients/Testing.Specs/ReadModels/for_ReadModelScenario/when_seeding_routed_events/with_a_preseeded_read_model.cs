// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_seeding_routed_events;

public class with_a_preseeded_read_model : Specification
{
    ReadModelScenario<ProjectedEventRoute> _scenario;
    ProjectedEventRoute _seeded;
    ProjectedEventRoute _result;
    EventSourceId _source;

    void Establish()
    {
        _scenario = new();
        _source = EventSourceId.New();
        _seeded = new(Guid.Parse(_source.Value), "pinned", EventSourceType.Default, EventStreamType.All, EventStreamId.Default, EventSequenceNumber.First);
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_source).OnStream("Order", "Payments", "2026-10").ReadModel(_seeded);
        _result = await _scenario.ReadModels.GetInstanceById<ProjectedEventRoute>(_source);
    }

    void Destroy() => _scenario.Dispose();

    [Fact] void should_return_the_unmodified_preseeded_model() => _result.ShouldEqual(_seeded);
}
