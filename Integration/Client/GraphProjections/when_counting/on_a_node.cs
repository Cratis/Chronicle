// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using context = Cratis.Chronicle.Integration.GraphProjections.when_counting.on_a_node.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_counting;

[Collection(GraphChronicleCollection.Name)]
public class on_a_node(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        void Establish()
        {
            Tell(OrganizationStory.Ada, new OrgPersonRegistered(OrganizationStory.Ada, "Ada", []));
            Tell(OrganizationStory.Ada, new OrgPointsAwarded(OrganizationStory.Ada, 10));
            Tell(OrganizationStory.Ada, new OrgPointsAwarded(OrganizationStory.Ada, 5));
            Tell(OrganizationStory.Ada, new OrgPointsRevoked(OrganizationStory.Ada, 3));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, Guid.NewGuid()));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Lobby, Guid.NewGuid()));
            Tell(OrganizationStory.Ada, new OrgPlaceVisited(OrganizationStory.Ada, OrganizationStory.Cafe, Guid.NewGuid()));
        }
    }

    [Fact] void should_add_and_subtract_event_amounts() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["score"].ShouldEqual(12L);
    [Fact] void should_count_events() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["visits"].ShouldEqual(3L);
}
