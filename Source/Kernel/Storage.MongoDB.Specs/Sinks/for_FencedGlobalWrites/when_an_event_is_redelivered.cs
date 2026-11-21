// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites;

[Collection(MongoDBCollection.Name)]
public class when_an_event_is_redelivered(MongoDBFixture fixture) : given.a_counter_shared_by_two_namespaces(fixture)
{
    BsonDocument _document;

    async Task Because()
    {
        await Increment(_sinkA, NamespaceA, KeyOf("counter"), 100);

        // The same event again for the same namespace, and an earlier sequence number from the other one.
        await Increment(_sinkA, NamespaceA, KeyOf("counter"), 100);
        await Increment(_sinkB, NamespaceB, KeyOf("counter"), 5);
        await Increment(_sinkB, NamespaceB, KeyOf("counter"), 5);
        _document = await StoredDocument("counter");
    }

    [Fact] void should_apply_each_event_once_per_namespace() => _document["count"].ToInt32().ShouldEqual(2);
    [Fact] void should_not_reject_the_lower_sequence_number_of_the_other_namespace() => _document["__lastHandled"]["namespace-b"].ToInt64().ShouldEqual(5);
    [Fact] void should_keep_the_higher_sequence_number_of_the_first_namespace() => _document["__lastHandled"]["namespace-a"].ToInt64().ShouldEqual(100);
}
