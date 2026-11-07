// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_counting.groups_incrementing_and_decrementing.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_counting;

[Collection(GraphChronicleCollection.Name)]
public class groups_incrementing_and_decrementing(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Bob, new OrgMemberAdded(OrganizationStory.Bob, OrganizationStory.Engineering));
            Tell(OrganizationStory.Cy, new OrgMemberAdded(OrganizationStory.Cy, OrganizationStory.Engineering));
            Tell(OrganizationStory.Bob, new OrgMemberRemoved(OrganizationStory.Bob, OrganizationStory.Engineering));
        }
    }

    [Fact] void should_increment_and_decrement() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering)!["memberCount"].ShouldEqual(2L);
    [Fact] void should_keep_the_property_set_by_the_creating_event() => Context.Graph.Node("Group", "groupId", OrganizationStory.Engineering)!["name"].ShouldEqual("Engineering");
}
