// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Services.ReadModels.for_ReadModels.when_getting_all_instances;

public class and_all_events_are_subscribed_alongside_removals_and_derivatives : given.a_projection_replay
{
    GetAllInstancesResponse _result;

    void Establish() => _definition = _definition with
    {
        SubscribesToAllEvents = true,
        From = new Dictionary<EventType, FromDefinition>(),
        FromDerivatives = [new FromDerivatives([Mapped], new FromDefinition(new Dictionary<PropertyPath, string>(), PropertyExpression.NotSet, null))],
        RemovedWith = new Dictionary<EventType, RemovedWithDefinition> { [Mapped] = new(PropertyExpression.NotSet, null) }
    };

    async Task Because() => _result = await _service.GetAllInstances(new()
    {
        EventStore = "test-store",
        Namespace = "test-namespace",
        ReadModelIdentifier = _readModelDefinition.Identifier,
        EventSequenceId = "event-log",
        EventCount = 3
    });

    [Fact] void should_replay_every_event_including_the_unmapped_type() => _result.ProcessedEventsCount.ShouldEqual(3UL);
}
