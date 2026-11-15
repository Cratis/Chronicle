// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_removing.a_node_with_detach.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_removing;

[Collection(GraphChronicleCollection.Name)]
public class a_node_with_detach(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Ada, new OrgPersonRemoved(OrganizationStory.Ada));
        }
    }

    [Fact] void should_delete_the_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada).ShouldBeNull();
    [Fact] void should_delete_the_edges_attached_to_it() => Context.Graph.Edges.ShouldBeEmpty();
    [Fact] void should_leave_the_nodes_at_the_other_end() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering).ShouldNotBeNull();
}
