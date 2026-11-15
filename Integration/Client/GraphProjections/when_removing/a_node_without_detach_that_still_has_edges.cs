// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_removing.a_node_without_detach_that_still_has_edges.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_removing;

[Collection(GraphChronicleCollection.Name)]
public class a_node_without_detach_that_still_has_edges(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Engineering, new OrgGroupDissolved(OrganizationStory.Engineering), fails: true);
        }
    }

    [Fact] void should_fail_the_partition_instead_of_dangling_the_edge() => Context.LastCompletion!.FailedPartitions.ShouldNotBeEmpty();
    [Fact] void should_keep_the_node() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering).ShouldNotBeNull();
    [Fact] void should_keep_the_edge() => Context.Graph.EdgesOf("MEMBER_OF").Count().ShouldEqual(1);
}
