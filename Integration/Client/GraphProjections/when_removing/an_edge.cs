// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_removing.an_edge.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_removing;

[Collection(GraphChronicleCollection.Name)]
public class an_edge(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Ada, new OrgMemberRemoved(OrganizationStory.Ada, OrganizationStory.Engineering));
        }
    }

    [Fact] void should_delete_the_edge() => Context.Graph.Edges.ShouldBeEmpty();
    [Fact] void should_leave_the_source_node() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada).ShouldNotBeNull();
    [Fact] void should_leave_the_target_node() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering).ShouldNotBeNull();
}
