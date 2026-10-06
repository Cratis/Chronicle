// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_mixed_all_events_are_replayed_without_a_limit : given.a_projection_replay
{
    GetAllInstancesResponse _result;

    void Establish() => _definition = _definition with { SubscribesToAllEvents = true };

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = ulong.MaxValue
    });

    [Fact] void should_replay_every_event_including_the_unmapped_type() => _result.ProcessedEventsCount.ShouldEqual(3UL);
}
