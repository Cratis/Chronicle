// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.ModelBound;
using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.GraphProjections.when_updating_the_projection_definition.and_an_element_is_removed.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_updating_the_projection_definition;

[Collection(GraphChronicleCollection.Name)]
public class and_an_element_is_removed(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        Type[] _elements = [.. ModelBoundOrganizationTypes.All];

        public int GroupsBefore;
        public int EdgesBefore;

        protected override IEnumerable<Type> Elements => _elements;

        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
        }

        async Task Because()
        {
            GroupsBefore = Graph.NodesOf("Group").Count();
            EdgesBefore = Graph.EdgesOf("MEMBER_OF").Count();
            var outcome = await EventStore.WaitForRegistration(TimeSpanFactory.FromSeconds(30));
            outcome.IsSuccess.ShouldBeTrue();

            _elements = [typeof(Person), typeof(Place), typeof(Visited), typeof(Greeted)];
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();
            var job = await EventStore.Projections.Replay(new ProjectionId("OrgModelBound"));
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await ReadGraph();
        }
    }

    [Fact] void should_have_had_the_group_and_the_membership_before() => (Context.GroupsBefore, Context.EdgesBefore).ShouldEqual((1, 1));
    [Fact] void should_sweep_the_nodes_the_definition_no_longer_declares() => Context.Graph.NodesOf("Group").ShouldBeEmpty();
    [Fact] void should_sweep_the_edges_the_definition_no_longer_declares() => Context.Graph.EdgesOf("MEMBER_OF").ShouldBeEmpty();
    [Fact] void should_keep_what_the_definition_still_declares() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada).ShouldNotBeNull();
}
