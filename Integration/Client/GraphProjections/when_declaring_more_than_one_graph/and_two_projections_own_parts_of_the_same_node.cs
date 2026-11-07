// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_declaring_more_than_one_graph.and_two_projections_own_parts_of_the_same_node.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_declaring_more_than_one_graph;

[Collection(GraphChronicleCollection.Name)]
public class and_two_projections_own_parts_of_the_same_node(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        protected override IEnumerable<Type> Elements => [typeof(MultipleGraphs.Person), typeof(MultipleGraphs.Scores.Person)];

        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Ada, new OrgPointsAwarded(OrganizationStory.Ada, 10));
            Tell(OrganizationStory.Ada, new OrgPointsRevoked(OrganizationStory.Ada, 4));
            Tell(OrganizationStory.Ada, new OrgPersonRenamed(OrganizationStory.Ada, "Ada L."));
        }
    }

    [Fact] void should_share_one_node() => Context.Graph.NodesOf("Person").Count().ShouldEqual(1);
    [Fact] void should_have_what_the_first_projection_owns() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["name"].ShouldEqual("Ada L.");
    [Fact] void should_have_what_the_second_projection_owns() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["score"].ShouldEqual(6L);
}
