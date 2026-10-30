// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_the_definition_is_new : given.a_projection_adding_historical_event_mappings
{
    void Establish() => _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
        .Returns(ProjectionDefinitionCompareResult.New);

    async Task Because() => await _grain.SetDefinition(_candidate);

    [Fact] void should_notify_subscribers() => _ = _subscriber.Received(1).OnProjectionDefinitionsChanged(Arg.Any<ProjectionDefinition>());
    [Fact] void should_not_schedule_a_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
}
