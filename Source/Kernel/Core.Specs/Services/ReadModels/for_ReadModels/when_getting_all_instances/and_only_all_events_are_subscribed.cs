// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_only_all_events_are_subscribed : given.a_projection_replay
{
    GetAllInstancesResponse _result;

    void Establish()
    {
        _definition = _definition with { SubscribesToAllEvents = true, From = new Dictionary<Concepts.Events.EventType, Concepts.Projections.Definitions.FromDefinition>() };
        _projection.GetEventTypes().Returns([]);
    }

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = 3
    });

    [Fact] void should_replay_every_event() => _result.ProcessedEventsCount.ShouldEqual(3UL);
}
