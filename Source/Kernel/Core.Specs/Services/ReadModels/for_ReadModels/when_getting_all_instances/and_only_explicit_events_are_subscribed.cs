// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_only_explicit_events_are_subscribed : given.a_projection_replay
{
    GetAllInstancesResponse _result;

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = 3
    });

    [Fact] void should_replay_only_the_explicit_event_type() => _result.ProcessedEventsCount.ShouldEqual(2UL);
}
