// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges.and_the_node_arrives_later.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges;

[Collection(GraphChronicleCollection.Name)]
public class and_the_node_arrives_later(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Bob, 1));
            Tell(OrganizationStory.Bob, new OrgPersonRegistered(OrganizationStory.Bob, "Bob", ["dev"]));
        }
    }

    [Fact] void should_complete_the_bare_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Bob)!["name"].ShouldEqual("Bob");
    [Fact] void should_give_it_a_watermark() => Context.Graph.Node("Person", "personId", OrganizationStory.Bob)!.Watermark.ShouldEqual((long)Context.Sequences[1]);
    [Fact] void should_keep_one_node_for_the_key() => Context.Graph.NodesOf("Person").Count(_ => _["personId"]?.ToString() == OrganizationStory.Bob.ToString()).ShouldEqual(1);
    [Fact] void should_keep_the_edge() => Context.Graph.EdgesOf("GREETED").Count().ShouldEqual(1);
}
