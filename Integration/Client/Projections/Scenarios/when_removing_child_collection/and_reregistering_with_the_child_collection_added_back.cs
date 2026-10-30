// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Recommendations;
using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection.and_reregistering_with_the_child_collection_added_back.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_removing_child_collection;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/5071: a client first registers a projection without a child
/// collection, a later build adds it back while the event history for it already exists. The automatic replay must run
/// over the new event types (not the previous ones) so the history ends up in the read model, without a recommendation.
/// </summary>
/// <param name="context">The context for the specification.</param>
[Collection(ChronicleCollection.Name)]
public class and_reregistering_with_the_child_collection_added_back(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ConversationProjection, Conversation>(chronicleFixture)
    {
        public Conversation ResultAfterReregistration;
        public IEnumerable<RecommendationDetailsResponse> Recommendations;

        public override IEnumerable<Type> EventTypes => [typeof(ConversationStarted), typeof(CommentAdded), typeof(ReactionGiven)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            ConversationProjection.WithReactions = false;
            services.AddSingleton(new ConversationProjection());
        }

        void Establish()
        {
            EventsToAppend.Add(new ConversationStarted("The conversation"));
            EventsToAppend.Add(new CommentAdded(Guid.NewGuid(), "First comment"));
        }

        async Task Because()
        {
            await WaitForClientRegistration();

            // The projection does not observe this event yet, so it is history that only a replay can bring in.
            var reaction = await EventStore.EventLog.Append(EventSourceId, new ReactionGiven(Guid.NewGuid(), "thumbs-up"));
            await DeactivateAllGrains();

            ConversationProjection.WithReactions = true;
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            // The replay starts once the observer has taken the new event types, and a job is removed the moment it
            // completes, so it may already be gone. When it is still there, waiting for it keeps a failed replay's
            // reason; either way the projection has handled the reaction only once the replay brought it in.
            await Projection.WaitTillSubscribed(TimeSpan.FromSeconds(30));
            foreach (var job in await EventStore.Jobs.TryFindJobsOfType("ReplayObserver", TimeSpan.FromSeconds(30)))
            {
                await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job.Id, TimeSpan.FromSeconds(30));
            }

            await Projection.WaitTillReachesEventSequenceNumber(reaction.SequenceNumber, TimeSpan.FromSeconds(30));
            ResultAfterReregistration = await GetReadModel(EventSourceId);

            var servicesAccessor = (IChronicleServicesAccessor)EventStore.Connection;
            Recommendations = await servicesAccessor.Services.Recommendations.GetRecommendations(new() { EventStore = EventStore.Name, Namespace = EventStore.Namespace }).EnsureSuccess();
        }
    }

    [Fact] void should_replay_the_history_of_the_added_child_collection() => Context.ResultAfterReregistration.Reactions.Count().ShouldEqual(1);
    [Fact] void should_keep_the_existing_comment() => Context.ResultAfterReregistration.Comments.Count().ShouldEqual(1);
    [Fact] void should_not_require_manual_replay_for_the_changed_definition() => Context.Recommendations.ShouldBeEmpty();
}
