// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.ModelBound;
using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.GraphProjections.when_updating_the_projection_definition.and_an_element_is_added.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_updating_the_projection_definition;

[Collection(GraphChronicleCollection.Name)]
public class and_an_element_is_added(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        Type[] _elements = [typeof(Person), typeof(Group), typeof(MemberOf)];

        public int PlacesBefore;
        public int VisitsBefore;

        protected override IEnumerable<Type> Elements => _elements;

        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, OrganizationStory.FirstVisit));
        }

        async Task Because()
        {
            PlacesBefore = Graph.NodesOf("Place").Count();
            VisitsBefore = Graph.EdgesOf("VISITED").Count();
            var outcome = await EventStore.WaitForRegistration(TimeSpanFactory.FromSeconds(30));
            outcome.IsSuccess.ShouldBeTrue();

            _elements = [.. ModelBoundOrganizationTypes.All];
            await EventStore.DiscoverAll();
            await EventStore.RegisterAll();
            var job = await EventStore.Projections.Replay(new ProjectionId("OrgModelBound"));
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await ReadGraph();
        }
    }

    [Fact] void should_not_have_the_added_elements_before() => (Context.PlacesBefore, Context.VisitsBefore).ShouldEqual((0, 0));
    [Fact] void should_build_the_added_nodes_from_the_events_already_in_the_log() => Context.Graph.Node("Place", "placeId", OrganizationStory.Lobby).ShouldNotBeNull();
    [Fact] void should_build_the_added_edges_from_the_events_already_in_the_log() => Context.Graph.EdgesBetween("VISITED", "personId", OrganizationStory.Ada, "placeId", OrganizationStory.Lobby).Count().ShouldEqual(1);
    [Fact] void should_keep_what_was_there() => Context.Graph.EdgesOf("MEMBER_OF").Count().ShouldEqual(1);
}
