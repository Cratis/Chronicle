// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Schemas;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_automatic_full_replay_is_not_allowed : given.a_projection_adding_historical_event_mappings
{
    IRecommendationsManager _recommendations;

    void Establish()
    {
        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.Get(_candidate.ReadModel).Returns(new ReadModelDefinition(
            _candidate.ReadModel,
            "read-models",
            "Read Model",
            ReadModelOwner.Client,
            ReadModelSource.User,
            ReadModelObserverType.Projection,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema>(),
            []));
    }

    [Theory]
    [InlineData(DefinitionEvolutionPolicy.PartialOnly)]
    [InlineData(DefinitionEvolutionPolicy.Manual)]
    async Task should_recommend_full_replay_without_replaying_in_place(DefinitionEvolutionPolicy policy)
    {
        _options.Value.Returns(new ChronicleOptions { Observers = new Observers { DefinitionEvolution = policy } });

        await _grain.SetDefinition(_candidate);
        await _silo.TimerRegistry.FireAllAsync();

        _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
        _observer.ReceivedCalls().ShouldBeEmpty();
        await _recommendations.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            Arg.Any<RecommendationDescription>(),
            Arg.Is<ReplayCandidateRequest>(request => request.ObserverId.Value == _candidate.Identifier.Value));
    }
}
