// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Schemas;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

/// <summary>
/// When the observer never takes the new event types, the grain gives up waiting and recommends a replay instead of
/// replaying over the wrong event types.
/// </summary>
public class and_the_observer_never_resubscribes : given.a_projection_adding_historical_event_mappings
{
    IRecommendationsManager _recommendations;

    void Establish()
    {
        SetObserverEventTypes([_existingEventType]);
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

    async Task Because()
    {
        await _grain.SetDefinition(_candidate);
        for (var i = 0; i < 100 && _silo.TimerRegistry.NumberOfActiveTimers > 0; i++)
        {
            await _silo.TimerRegistry.FireAllAsync();
        }
    }

    [Fact] async Task should_never_replay() => await _observer.DidNotReceive().Replay();
    [Fact] async Task should_recommend_a_replay() => await _recommendations.Received(1).Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
        Arg.Any<RecommendationDescription>(),
        Arg.Is<ReplayCandidateRequest>(request => request.ObserverId.Value == _candidate.Identifier.Value));
}
