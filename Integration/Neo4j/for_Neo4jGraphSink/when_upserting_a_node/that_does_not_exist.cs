// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node.that_does_not_exist.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_a_node;

/// <summary>
/// Upserting a node that does not exist on a real Neo4j server creates it, with the bookkeeping the sink keeps for itself.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class that_does_not_exist(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Stored = default!;
        public GraphNode Found = default!;
        public long Nodes;

        async Task Because()
        {
            await _run.Apply(7, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"), ("visits", 3))));
            Stored = (await StoredPerson("p1"))!;
            Found = (await _sink.FindNode(Owner, Person("p1")))!;
            Nodes = await CountNodes("Person");
        }
    }

    [Fact] void should_create_one_node() => Context.Nodes.ShouldEqual(1L);
    [Fact] void should_store_the_key_under_the_name_of_the_definition() => Context.Stored["personId"].ShouldEqual("p1");
    [Fact] void should_store_the_properties() => Context.Stored["name"].ShouldEqual("Ada");
    [Fact] void should_store_whole_numbers_as_integers() => Context.Stored["visits"].ShouldEqual(3L);
    [Fact] void should_stamp_the_watermark_with_the_sequence_number() => Context.Stored["_watermark"].ShouldEqual(7L);
    [Fact] void should_stamp_the_owner() => Context.Stored["_owner"].ShouldEqual("owner");
    [Fact] void should_stamp_the_generation() => Context.Stored["_generation"].ShouldEqual(0L);
    [Fact] void should_stamp_the_namespace_only_when_namespaces_share_the_database() => Context.Stored.ContainsKey("_namespace").ShouldEqual(Context.IsSingleDatabase);
    [Fact] void should_find_the_node() => Context.Found.ShouldNotBeNull();
    [Fact] void should_report_the_watermark_when_finding_the_node() => Context.Found.Watermark.Value.ShouldEqual(7UL);
    [Fact] void should_not_report_the_bookkeeping_as_properties() => Context.Found.Properties.Keys.Any(_ => _.StartsWith('_')).ShouldBeFalse();
    [Fact] void should_report_the_properties_when_finding_the_node() => Context.Found.Properties["name"].ShouldEqual("Ada");
}
