// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges.with_an_identifying_property.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges;

[Collection(GraphChronicleCollection.Name)]
public class with_an_identifying_property(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, Guid.Parse("d1000000-0000-4000-8000-000000000001")));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, Guid.Parse("d1000000-0000-4000-8000-000000000002")));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, Guid.Parse("d1000000-0000-4000-8000-000000000001")));
        }
    }

    [Fact] void should_have_an_edge_per_identity() => Context.Graph.EdgesBetween("VISITED", "personId", OrganizationStory.Ada, "placeId", OrganizationStory.Lobby).Count().ShouldEqual(2);
    [Fact] void should_store_the_identity_on_the_edge() => Context.Graph.EdgesOf("VISITED").Select(_ => _["visitId"]?.ToString()).Order().ShouldEqual(new[] { Guid.Parse("d1000000-0000-4000-8000-000000000001").ToString(), Guid.Parse("d1000000-0000-4000-8000-000000000002").ToString() }.Order());
    [Fact] void should_not_duplicate_the_end_nodes() => (Context.Graph.NodesOf("Person").Count(), Context.Graph.NodesOf("Place").Count()).ShouldEqual((1, 1));
    [Fact] void should_still_count_every_event_on_the_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["visits"].ShouldEqual(3L);
}
