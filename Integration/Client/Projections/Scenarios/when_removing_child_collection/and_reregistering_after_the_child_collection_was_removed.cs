// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Recommendations;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection.and_reregistering_after_the_child_collection_was_removed.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection;

/// <summary>
/// Reproduces the production failure behind https://github.com/Cratis/Chronicle/issues/3722: a client first
/// registers a projection with two child collections, a later client build re-registers it with one of the
/// child collections removed. The stored definition must follow the change, a replay recommendation must be
/// raised, and the observer subscriber must be able to build its pipeline and keep projecting.
/// </summary>
/// <param name="context">The context for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class and_reregistering_after_the_child_collection_was_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ConversationProjection, Conversation>(chronicleFixture)
    {
        public Conversation ResultAfterReregistration;
        public Contracts.Projections.ProjectionDefinition StoredDefinition;
        public IEnumerable<RecommendationDetailsResponse> Recommendations;
        public CommentAdded SecondComment;

        public override IEnumerable<Type> EventTypes => [typeof(ConversationStarted), typeof(CommentAdded), typeof(ReactionGiven)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            ConversationProjection.WithReactions = true;
            services.AddSingleton(new ConversationProjection());
        }

        void Establish()
        {
            EventsToAppend.Add(new ConversationStarted("The conversation"));
            EventsToAppend.Add(new CommentAdded(Guid.NewGuid(), "First comment"));
            EventsToAppend.Add(new ReactionGiven(Guid.NewGuid(), "thumbs-up"));
            SecondComment = new CommentAdded(Guid.NewGuid(), "Second comment");
        }

        async Task Because()
        {
            // Simulate the production sequence: the projection-related grains go idle and deactivate,
            // then a new client build re-registers the projection without the child collection.
            await DeactivateAllGrains();

            ConversationProjection.WithReactions = false;
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            await Projection.WaitTillSubscribed();

            var appendResult = await EventStore.EventLog.Append(EventSourceId, SecondComment);
            await Projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);
            ResultAfterReregistration = await GetReadModel(EventSourceId);

            var servicesAccessor = (IChronicleServicesAccessor)EventStore.Connection;
            var definitions = await servicesAccessor.Services.Projections.GetAllDefinitions(new() { EventStore = EventStore.Name });
            var projectionId = EventStore.Projections.GetProjectionIdForModel<Conversation>();
            StoredDefinition = definitions.Single(definition => definition.Identifier == projectionId.Value);
            Recommendations = await servicesAccessor.Services.Recommendations.GetRecommendations(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace }).EnsureSuccess();
        }
    }

    [Fact] void should_keep_projecting_after_reregistration() => Context.ResultAfterReregistration.ShouldNotBeNull();
    [Fact] void should_project_the_comment_added_after_reregistration() => Context.ResultAfterReregistration.Comments.Select(comment => comment.Text).ShouldContain(Context.SecondComment.Text);
    [Fact] void should_store_the_definition_without_the_removed_child() => Context.StoredDefinition.Children.Keys.Any(key => key.Equals("reactions", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_keep_the_remaining_child_in_the_stored_definition() => Context.StoredDefinition.Children.Keys.Any(key => key.Equals("comments", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_recommend_replay_for_the_changed_definition() => Context.Recommendations.Any(recommendation => recommendation.Description.Contains("Projection definition has changed")).ShouldBeTrue();
}
