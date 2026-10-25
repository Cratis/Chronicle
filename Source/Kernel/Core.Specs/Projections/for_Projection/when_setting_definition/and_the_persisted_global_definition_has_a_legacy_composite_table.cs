// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Orleans.TestKit;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

/// <summary>
/// A global projection keeps writing the legacy key format, because no namespace-local replay can rebuild its shared sink.
/// The operator is told through a recommendation instead.
/// </summary>
public class and_the_persisted_global_definition_has_a_legacy_composite_table : given.a_projection_grain_with_replay_on_definition_change
{
    IRecommendationsManager _recommendations;
    IObserver _observer;

    void Establish()
    {
        _definition = _definition with { Scope = ProjectionScope.Global };
        _comparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>())
            .Returns(ProjectionDefinitionCompareResult.Same);
        _namespaces.GetAll().Returns(["first", "second"]);

        var readModel = for_ProjectionsManager.given.a_projections_manager_grain.CreateReadModelDefinition(_definition.ReadModel) with
        {
            Schemas = new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = JsonSchema.FromJson("{\"type\":\"object\"}") }
        };
        _storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([readModel]);

        var sink = Substitute.For<ISink, ICanReportKeyFormatMigration>();
        ((ICanReportKeyFormatMigration)sink).RequiresKeyFormatMigration().Returns(true);
        _storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(EventStoreNamespaceName.NotSet).Sinks.GetFor(Arg.Any<Concepts.ReadModels.ReadModelDefinition>()).Returns(sink);

        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);
        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
    }

    async Task Because()
    {
        await _grain.SetDefinition(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_not_schedule_a_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] async Task should_not_replay_a_namespace() => await _observer.DidNotReceive().Replay();

    [Fact]
    async Task should_recommend_a_replay_for_each_namespace() => await _recommendations.Received(2)
        .Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            GlobalProjectionReplay.RefusalReason,
            Arg.Is<ReplayCandidateRequest>(_ => _.ObserverKey.ObserverId == _definition.Identifier.Value));
}
