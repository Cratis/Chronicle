// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;

namespace Cratis.Chronicle.MongoDB.Integration.Sinks.for_Sink.when_redelivering_to_a_source_keyed_join_projection;

[Collection(MongoDBCollection.Name)]
public class and_a_resolved_join_is_on_a_property_inside_an_object_the_event_sets(ChronicleInProcessFixture fixture) : given.a_source_keyed_join_projection(fixture)
{
    BsonDocument _created;

    async Task Because()
    {
        using var sink = CreateSink(UnfencedSinkWrites.Instance);
        var customer = new ExpandoObject();
        ((IDictionary<string, object?>)customer)["id"] = "A";
        var resolved = new ResolvedJoin(new ExpandoObject(), "A", new PropertyPath("customer.id"), ArrayIndexers.NoIndexers, [Direct(("count", 9))], JoinEvent(3UL));
        var wholeObject = new PropertiesChanged<ExpandoObject>(new ExpandoObject(), [new PropertyDifference(new PropertyPath("customer"), null, customer)]);
        await sink.ApplyChanges(new Key("fresh-customer", ArrayIndexers.NoIndexers), ChangesetOf(OwnEvent(10UL, "fresh-customer"), wholeObject, resolved), 10UL, SinkWriteMode.OncePerSourceEvent);
        _created = await Read("fresh-customer");
    }

    [Fact] void should_store_the_object() => _created["customer"]["id"].AsString.ShouldEqual("A");
    [Fact] void should_apply_the_join_to_the_reference_the_same_write_sets() => _created["count"].ToInt32().ShouldEqual(9);
}
