// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values.that_look_like_cypher.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_writing_values;

/// <summary>
/// Keys and values are parameters, never part of the text of a statement, so text that looks like Cypher is stored as it is.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class that_look_like_cypher(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Hostile;
        public IReadOnlyDictionary<string, object>? Safe;
        public long People;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("safe"), Props(("personId", "safe"), ("name", "Safe"))));
            await _run.Apply(2, new UpsertNode(Person(HostileKey), Props(("personId", HostileKey), ("name", HostileName))));
            await _run.Apply(3, new SetProperties(Person(HostileKey), Props(("visits", 1))), new UpsertEdge(MemberOf(HostileKey, "g`1"), Props(("since", HostileName))));
            Hostile = await StoredPerson(HostileKey);
            Safe = await StoredPerson("safe");
            People = await CountNodes("Person");
        }
    }

    const string HostileKey = "p1'}) DETACH DELETE (n) //";
    const string HostileName = "`}) MATCH (x) DETACH DELETE x; //\" \\ \n \u2603 \ud83d\ude00";

    [Fact] void should_store_the_key_as_it_is() => Context.Hostile!["personId"].ShouldEqual(HostileKey);
    [Fact] void should_store_the_value_as_it_is() => Context.Hostile!["name"].ShouldEqual(HostileName);
    [Fact] void should_not_run_the_text_of_a_value_as_a_statement() => Context.Safe.ShouldNotBeNull();
    [Fact] void should_have_one_node_for_each_person() => Context.People.ShouldEqual(2L);
}
