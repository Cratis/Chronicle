// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites;

[Collection(MongoDBCollection.Name)]
public class when_two_namespaces_create_the_same_counter(MongoDBFixture fixture) : given.a_counter_shared_by_two_namespaces(fixture)
{
    Exception _exception;
    BsonDocument _document;
    long _documentCount;

    async Task Because()
    {
        // Both read an absent instance, then both write.
        var readByA = await _sinkA.FindOrDefault(KeyOf("counter"), SinkWriteMode.FencedGlobal) ?? new ExpandoObject();
        var readByB = await _sinkB.FindOrDefault(KeyOf("counter"), SinkWriteMode.FencedGlobal) ?? new ExpandoObject();

        await _sinkA.ApplyChanges(KeyOf("counter"), CountSetTo(readByA, NamespaceA, 1), 1, SinkWriteMode.FencedGlobal);
        _exception = await Catch.Exception(() => _sinkB.ApplyChanges(KeyOf("counter"), CountSetTo(readByB, NamespaceB, 1), 1, SinkWriteMode.FencedGlobal));

        _document = await StoredDocument("counter");
        _documentCount = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_refuse_the_second_creation() => _exception.ShouldBeOfExactType<SinkWriteConflictException>();
    [Fact] void should_keep_the_first_creation() => _document["count"].ToInt32().ShouldEqual(1);
    [Fact] void should_have_one_document() => _documentCount.ShouldEqual(1);
}
