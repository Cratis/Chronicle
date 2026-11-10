// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values.of_every_type.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values;

/// <summary>
/// The primitives and the lists of them a graph can hold survive a round trip through a real server, and what a graph cannot hold is refused.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class of_every_type(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public GraphNode Found = default!;
        public IReadOnlyDictionary<string, object> Stored = default!;
        public Exception? Unsupported;
        public DateTimeOffset Moment = new(2026, 3, 4, 5, 6, 7, TimeSpan.FromHours(2));
        public Guid Identifier = Guid.NewGuid();

        async Task Because()
        {
            var node = new NodeReference("Person", "p1");
            await _run.Apply(
                1,
                new UpsertNode(node, Props(
                    ("personId", "p1"),
                    ("flag", true),
                    ("ratio", 1.5d),
                    ("amount", 2.25m),
                    ("count", 42),
                    ("tags", new[] { "a", "b" }),
                    ("numbers", new List<long> { 1, 2 }),
                    ("when", Moment),
                    ("identifier", Identifier),
                    ("nothing", null))));
            Found = (await _sink.FindNode(Owner, node))!;
            Stored = (await StoredPerson("p1"))!;
            Unsupported = await Catch.Exception(() => _run.Apply(2, new SetProperties(node, Props(("nested", new Dictionary<string, object> { ["a"] = 1 })))));
        }
    }

    [Fact] void should_round_trip_a_boolean() => Context.Found.Properties["flag"].ShouldEqual(true);
    [Fact] void should_round_trip_a_double() => Context.Found.Properties["ratio"].ShouldEqual(1.5d);
    [Fact] void should_store_a_decimal_as_a_double() => Context.Found.Properties["amount"].ShouldEqual(2.25d);
    [Fact] void should_store_a_whole_number_as_an_integer() => Context.Found.Properties["count"].ShouldEqual(42L);
    [Fact] void should_round_trip_a_list_of_strings() => ((IEnumerable<object?>)Context.Found.Properties["tags"]!).ShouldContainOnly("a", "b");
    [Fact] void should_round_trip_a_list_of_numbers() => ((IEnumerable<object?>)Context.Found.Properties["numbers"]!).ShouldContainOnly(1L, 2L);
    [Fact] void should_round_trip_a_moment_in_time() => ((DateTimeOffset)Context.Found.Properties["when"]!).ShouldEqual(Context.Moment);
    [Fact] void should_keep_the_offset_of_a_moment_in_time() => ((DateTimeOffset)Context.Found.Properties["when"]!).Offset.ShouldEqual(Context.Moment.Offset);
    [Fact] void should_store_a_guid_as_its_text() => Context.Found.Properties["identifier"].ShouldEqual(Context.Identifier.ToString("D"));
    [Fact] void should_not_store_a_null() => Context.Stored.ContainsKey("nothing").ShouldBeFalse();
    [Fact] void should_refuse_a_value_that_a_graph_cannot_store() => Context.Unsupported.ShouldBeOfExactType<UnsupportedGraphValue>();
}
