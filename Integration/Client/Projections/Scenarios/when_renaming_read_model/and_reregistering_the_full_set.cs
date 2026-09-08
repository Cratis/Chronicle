// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_renaming_read_model.and_reregistering_the_full_set.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_renaming_read_model;

/// <summary>
/// Covers the retirement behavior behind https://github.com/Cratis/Chronicle/issues/3725: a read model is renamed
/// (moved to another namespace), keeping its type name and thus its container name. When the client re-registers
/// its full set, the old projection must be retired - its observer stopped and its definition removed - and the
/// successor targeting the same container must get a replay recommendation. The container itself is never dropped.
/// </summary>
/// <param name="context">The context for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class and_reregistering_the_full_set(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        readonly List<Type> _projectionTypes = [typeof(original.BoardProjection)];

        public EventSourceId BoardId;
        public original.Board ResultBeforeRename;
        public IEnumerable<string> DefinitionIdentifiers;
        public IEnumerable<RecommendationDetailsResponse> Recommendations;
        public ObserverInformation OrphanObserver;

        public override IEnumerable<Type> Projections => _projectionTypes;
        public override IEnumerable<Type> EventTypes => [typeof(BoardNamed)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new original.BoardProjection());
            services.AddSingleton(new renamed.BoardProjection());
        }

        void Establish() => BoardId = Guid.NewGuid().ToString();

        async Task Because()
        {
            var originalProjection = EventStore.Projections.GetHandlerFor<original.BoardProjection>();
            await originalProjection.WaitTillSubscribed();

            var appendResult = await EventStore.EventLog.Append(BoardId, new BoardNamed("The board"));
            await originalProjection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            ResultBeforeRename = await EventStore.ReadModels.GetInstanceById<original.Board>(BoardId);

            // Deploy the rename: the client now declares only the renamed read model and its projection,
            // and re-registers its full set - read models first, then projections, like RegisterAll does.
            _projectionTypes.Clear();
            _projectionTypes.Add(typeof(renamed.BoardProjection));
            await EventStore.Projections.Discover();
            await EventStore.ReadModels.Register();
            await EventStore.Projections.Register();

            var successorProjection = EventStore.Projections.GetHandlerFor<renamed.BoardProjection>();
            await successorProjection.WaitTillSubscribed();

            var servicesAccessor = (IChronicleServicesAccessor)EventStore.Connection;
            var definitions = await servicesAccessor.Services.Projections.GetAllDefinitions(new() { EventStore = EventStore.Name });
            DefinitionIdentifiers = definitions.Select(definition => definition.Identifier).ToArray();
            Recommendations = await servicesAccessor.Services.Recommendations.GetRecommendations(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace }).EnsureSuccess();
            OrphanObserver = await servicesAccessor.Services.Observers.GetObserverInformation(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                ObserverId = typeof(original.BoardProjection).FullName!,
                EventSequenceId = EventSequences.EventSequenceId.Log
            });
        }
    }

    [Fact] void should_project_the_board_before_the_rename() => Context.ResultBeforeRename.Name.ShouldEqual("The board");
    [Fact] void should_remove_the_retired_projection_definition() => Context.DefinitionIdentifiers.ShouldNotContain(typeof(original.BoardProjection).FullName);
    [Fact] void should_keep_the_successor_projection_definition() => Context.DefinitionIdentifiers.ShouldContain(typeof(renamed.BoardProjection).FullName);
    [Fact] void should_stop_the_retired_observer() => Context.OrphanObserver.IsSubscribed.ShouldBeFalse();
    [Fact] void should_recommend_replaying_the_successor_sharing_the_container() => Context.Recommendations.Any(recommendation => recommendation.Description.Contains("retired") && recommendation.Description.Contains("Boards", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
}
