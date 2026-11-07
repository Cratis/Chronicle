// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_nodes.and_a_later_event_updates_it.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_nodes;

[Collection(GraphChronicleCollection.Name)]
public class and_a_later_event_updates_it(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", ["admin"]));
            Tell(OrganizationStory.Ada, new OrgPersonRenamed(OrganizationStory.Ada, "Ada L."));
        }
    }

    [Fact] void should_still_have_one_node() => Context.Graph.Nodes.Count.ShouldEqual(1);
    [Fact] void should_update_the_property() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["name"].ShouldEqual("Ada L.");
    [Fact] void should_keep_what_the_later_event_does_not_map() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["kind"].ShouldEqual("person");
    [Fact] void should_move_the_watermark() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!.Watermark.ShouldEqual((long)Context.Sequences[1]);
}
