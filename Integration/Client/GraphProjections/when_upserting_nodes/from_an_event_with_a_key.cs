// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_nodes.from_an_event_with_a_key.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_nodes;

[Collection(GraphChronicleCollection.Name)]
public class from_an_event_with_a_key(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", ["admin", "dev"]));
        }
    }

    [Fact] void should_create_the_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada).ShouldNotBeNull();
    [Fact] void should_create_nothing_else() => Context.Graph.Nodes.Count.ShouldEqual(1);
    [Fact] void should_map_a_property_from_the_event() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["name"].ShouldEqual("Ada");
    [Fact] void should_map_an_array_of_primitives() => string.Join(',', (IEnumerable<object>)Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["tags"]!).ShouldEqual("admin,dev");
    [Fact] void should_map_a_property_from_the_event_context() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["registered"].ShouldEqual(OrganizationStory.Start);
    [Fact] void should_map_a_constant() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["kind"].ShouldEqual("person");
    [Fact] void should_write_the_watermark() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!.Watermark.ShouldEqual((long)Context.Sequences[0]);
    [Fact] void should_stamp_the_owner() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!.Owner.ShouldNotBeNull();
    [Fact] void should_stamp_the_namespace_in_single_database_mode_only() => (Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!.Namespace is not null).ShouldEqual(Context.IsSingleDatabase);
}
