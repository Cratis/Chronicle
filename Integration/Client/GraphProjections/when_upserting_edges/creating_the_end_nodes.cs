// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges.creating_the_end_nodes.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges;

[Collection(GraphChronicleCollection.Name)]
public class creating_the_end_nodes(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Bob, 1));
        }
    }

    [Fact] void should_create_the_edge() => Context.Graph.EdgesOf("GREETED").Count().ShouldEqual(1);
    [Fact] void should_create_the_missing_end_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Bob).ShouldNotBeNull();
    [Fact] void should_create_it_bare() => Context.Graph.Node("Person", "personId", OrganizationStory.Bob)!.Properties.Keys.ShouldContainOnly("personId");
    [Fact] void should_not_give_it_a_watermark() => Context.Graph.Node("Person", "personId", OrganizationStory.Bob)!.Watermark.ShouldBeNull();
    [Fact] void should_leave_the_existing_end_node_alone() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["name"].ShouldEqual("Ada");
}
