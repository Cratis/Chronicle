// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties.with_a_sequence_number_at_or_below_the_watermark.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties;

/// <summary>
/// A set of properties by an event at or below the watermark changes nothing.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_a_sequence_number_at_or_below_the_watermark(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Stored = default!;

        async Task Because()
        {
            await _run.Apply(4, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
            await _run.Apply(4, new SetProperties(Person("p1"), Props(("name", "Redelivered"))));
            await _run.Apply(2, new SetProperties(Person("p1"), Props(("name", "Late"))));
            Stored = (await StoredPerson("p1"))!;
        }
    }

    [Fact] void should_keep_the_properties_of_the_event_that_wrote_last() => Context.Stored["name"].ShouldEqual("Ada");
    [Fact] void should_keep_the_watermark() => Context.Stored["_watermark"].ShouldEqual(4L);
}
