// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.GraphProjections.Organization;
using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.GraphProjections.when_replaying.and_the_counters_are_rebuilt.context;

namespace Cratis.Chronicle.Integration.GraphProjections.when_replaying;

[Collection(GraphChronicleCollection.Name)]
public class and_the_counters_are_rebuilt(context context) : Given<context>(context)
{
    public class context(GraphChronicleFixture fixture) : a_model_bound_organization(fixture)
    {
        public IReadOnlyList<string> Before = [];

        void Establish()
        {
            foreach (var step in OrganizationStory.Steps)
            {
                Tell(Guid.Parse(step.EventSourceId.Value), step.Event);
            }
        }

        async Task Because()
        {
            Before = Graph.Describe();
            var job = await EventStore.Projections.Replay(new ProjectionId("OrgModelBound"));
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await ReadGraph();
        }
    }

    [Fact] void should_have_a_graph_before_the_replay() => Context.Before.ShouldNotBeEmpty();
    [Fact] void should_leave_the_same_graph() => Assert.Equal(Context.Before, Context.Graph.Describe());
    [Fact] void should_rebuild_counters_from_zero_instead_of_counting_on_top() => Context.Graph.Node("Person", "personId", OrganizationStory.Ada)!["score"].ShouldEqual(12L);
    [Fact] void should_keep_the_edge_counters() => Context.Graph.EdgesBetween("GREETED", "personId", OrganizationStory.Ada, "personId", OrganizationStory.Bob).Single()["times"].ShouldEqual(1L);
    [Fact] void should_write_the_watermarks_of_the_replay() => Context.Graph.Nodes.Where(_ => _.Watermark is not null).ShouldNotBeEmpty();
}
