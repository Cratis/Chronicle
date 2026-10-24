// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_join_precedes_another_event_in_the_same_page(MongoDBFixture fixture) : given.a_page_with_direct_mutations(fixture)
{
    BsonDocument _prepared;
    BsonDocument _unprepared;

    async Task Because()
    {
        _unprepared = await RunPage("unprepared", false);
        _prepared = await RunPage("prepared", true);
    }

    [Fact] void should_match_unprepared_processing() => _prepared.ShouldEqual(_unprepared);
    [Fact] void should_use_the_state_written_by_the_join() => _prepared["count"].ToInt32().ShouldEqual(43);

    async Task<BsonDocument> RunPage(string container, bool prepare)
    {
        var (sink, collection) = await BeginPage(container, prepare);
        var joined = Changes(new ExpandoObject());
        joined.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => 42)], ArrayIndexers.NoIndexers);
        var join = Changes(new ExpandoObject());
        join.Add(new Joined(new ExpandoObject(), "join", new("joinKey"), ArrayIndexers.NoIndexers, joined.Changes));
        await sink.ApplyChanges(new Key("join", ArrayIndexers.NoIndexers), join, 1UL);
        var state = (await sink.FindOrDefault(_root))!;
        var nextCount = Convert.ToInt32(((IDictionary<string, object?>)state)["count"], CultureInfo.InvariantCulture) + 1;
        var next = Changes(state);
        next.SetProperties([PropertyMappers.FromEventValueProvider("count", _ => nextCount)], ArrayIndexers.NoIndexers);
        await sink.ApplyChanges(_root, next, 2UL);
        await sink.EndBulk();
        return await collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }
}
