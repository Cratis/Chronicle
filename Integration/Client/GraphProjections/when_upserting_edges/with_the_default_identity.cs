// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges.with_the_default_identity.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_upserting_edges;

[Collection(GraphChronicleCollection.Name)]
public class with_the_default_identity(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Engineering, new OrgGroupCreated(OrganizationStory.Engineering, "Engineering"));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
            Tell(OrganizationStory.Ada, new OrgMemberAdded(OrganizationStory.Ada, OrganizationStory.Engineering));
        }
    }

    [Fact] void should_have_one_edge_of_the_type_between_the_two_nodes() => Context.Graph.EdgesBetween("MEMBER_OF", "personId", OrganizationStory.Ada, "groupId", OrganizationStory.Engineering).Count().ShouldEqual(1);
    [Fact] void should_write_the_watermark_of_the_latest_event() => Context.Graph.EdgesOf("MEMBER_OF").Single().Watermark.ShouldEqual((long)Context.Sequences[2]);
}
