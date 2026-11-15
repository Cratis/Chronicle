// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_counting.on_an_edge.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_counting;

[Collection(GraphChronicleCollection.Name)]
public class on_an_edge(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Bob, 1));
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Bob, 2));
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Bob, 3));
            Tell(OrganizationStory.Ada, new OrgGreetingRetracted(OrganizationStory.Ada, OrganizationStory.Bob, 2));
            Tell(OrganizationStory.Ada, new OrgPersonGreeted(OrganizationStory.Ada, OrganizationStory.Cy, 9));
        }
    }

    [Fact] void should_have_one_edge_per_pair() => Context.Graph.EdgesOf("GREETED").Count().ShouldEqual(2);
    [Fact] void should_increment_and_decrement_the_edge() => Context.Graph.EdgesBetween("GREETED", "personId", OrganizationStory.Ada, "personId", OrganizationStory.Bob).Single()["times"].ShouldEqual(2L);
    [Fact] void should_add_and_subtract_event_amounts_on_the_edge() => Context.Graph.EdgesBetween("GREETED", "personId", OrganizationStory.Ada, "personId", OrganizationStory.Bob).Single()["warmth"].ShouldEqual(4L);
    [Fact] void should_count_the_other_edge_on_its_own() => Context.Graph.EdgesBetween("GREETED", "personId", OrganizationStory.Ada, "personId", OrganizationStory.Cy).Single()["times"].ShouldEqual(1L);
}
