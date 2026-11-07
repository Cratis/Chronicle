// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_removing.a_node_without_detach_that_has_no_edges.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_removing;

[Collection(GraphChronicleCollection.Name)]
public class a_node_without_detach_that_has_no_edges(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Operations, new OrgGroupCreated(OrganizationStory.Operations, "Operations"));
            Tell(OrganizationStory.Engineering, new OrgGroupDissolved(OrganizationStory.Engineering));
        }
    }

    [Fact] void should_delete_the_node() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering).ShouldBeNull();
    [Fact] void should_leave_the_other_node() => Context.Graph.Node("Group", "groupId", OrganizationStory.Operations).ShouldNotBeNull();
}
