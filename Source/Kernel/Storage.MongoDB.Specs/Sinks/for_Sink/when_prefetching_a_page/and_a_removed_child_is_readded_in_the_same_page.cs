// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Properties;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_prefetching_a_page;

[Collection(MongoDBCollection.Name)]
public class and_a_removed_child_is_readded_in_the_same_page(MongoDBFixture fixture) : given.a_page_with_direct_mutations(fixture)
{
    BsonDocument _prepared;
    BsonDocument _unprepared;

    async Task Because()
    {
        _unprepared = await RunPage("unprepared", false);
        _prepared = await RunPage("prepared", true);
    }

    [Fact] void should_match_unprepared_processing() => _prepared.ShouldEqual(_unprepared);
    [Fact] void should_readd_the_removed_child() => _prepared["children"].AsBsonArray.Count.ShouldEqual(1);
    [Fact] void should_readd_the_correct_child() => _prepared["children"][0]["childId"].AsString.ShouldEqual("c");

    async Task<BsonDocument> RunPage(string container, bool prepare)
    {
        var (sink, collection) = await BeginPage(container, prepare);
        var removal = Changes(new ExpandoObject());
        removal.Add(new ChildRemovedFromAll(new("children"), new("childId"), "c", ArrayIndexers.NoIndexers));
        await sink.ApplyChanges(new Key("c", ArrayIndexers.NoIndexers), removal, 1UL);
        var state = (await sink.FindOrDefault(_root))!;
        var children = ((IEnumerable<object>)((IDictionary<string, object?>)state)["children"]!).ToList();
        if (!children.Cast<IDictionary<string, object?>>().Any(child => Equals(child["childId"], "c")))
        {
            children.Add(Child());
            var readd = Changes(state);
            readd.SetProperties([PropertyMappers.FromEventValueProvider("children", _ => children)], ArrayIndexers.NoIndexers);
            await sink.ApplyChanges(_root, readd, 2UL);
        }
        await sink.EndBulk();
        return await collection.Find(new BsonDocument("_id", "k")).SingleAsync();
    }
}
