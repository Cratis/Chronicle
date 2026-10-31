// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_the_definition_changes;

public class and_the_sink_is_global : given.a_projection_grain_with_replay_on_definition_change
{
    IRecommendationsManager _recommendations;

    void Establish()
    {
        _definition = _definition with { Scope = ProjectionScope.Global };
        _namespaces.GetAll().Returns(["first", "second"]);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.Get(_definition.ReadModel).Returns(
            for_ProjectionsManager.given.a_projections_manager_grain.CreateReadModelDefinition(_definition.ReadModel));
        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);
    }

    async Task Because()
    {
        await _grain.SetDefinition(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_schedule_automatic_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_not_replay_a_namespace() => await _observer.DidNotReceive().Replay();

    [Fact]
    async Task should_retain_an_explanatory_recommendation_for_each_namespace() => await _recommendations.Received(2)
        .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            GlobalProjectionReplay.RefusalReason,
            Arg.Is<ReplayCandidateRequest>(_ => _.ObserverKey.ObserverId == _definition.Identifier.Value));
}
