// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_FencedGlobalWrites;

[Collection(MongoDBCollection.Name)]
public class when_two_namespaces_increment_the_same_counters_concurrently(MongoDBFixture fixture) : given.a_counter_shared_by_two_namespaces(fixture)
{
    const int Counters = 3;
    const int IncrementsPerNamespace = 25;

    readonly string[] _ids = [.. Enumerable.Range(0, Counters).Select(_ => $"counter-{_}")];
    BsonDocument[] _documents = [];
    long _documentCount;

    async Task Establish()
    {
        // One counter is a legacy document that has none of the bookkeeping.
        await _collection.InsertOneAsync(new BsonDocument { { "_id", _ids[0] }, { "count", 10 } });
    }

    async Task Because()
    {
        var writers = _ids.SelectMany(id => new[]
        {
            Task.Run(async () =>
            {
                for (ulong n = 1; n <= IncrementsPerNamespace; n++)
                {
                    await IncrementWithRetry(_sinkA, NamespaceA, KeyOf(id), n);
                }
            }),
            Task.Run(async () =>
            {
                for (ulong n = 1; n <= IncrementsPerNamespace; n++)
                {
                    await IncrementWithRetry(_sinkB, NamespaceB, KeyOf(id), n);
                }
            })
        });
        await Task.WhenAll(writers);

        _documents = [.. await Task.WhenAll(_ids.Select(StoredDocument))];
        _documentCount = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    [Fact] void should_lose_no_increment() => _documents.Select(_ => _["count"].ToInt32()).ShouldEqual([10 + (2 * IncrementsPerNamespace), 2 * IncrementsPerNamespace, 2 * IncrementsPerNamespace]);
    [Fact] void should_count_one_revision_per_write() => _documents.Select(_ => _["__revision"].ToInt64()).ShouldEachConformTo(_ => _ == 2 * IncrementsPerNamespace);
    [Fact] void should_track_the_last_handled_event_per_namespace() => _documents.ShouldEachConformTo(_ =>
        _["__lastHandled"]["namespace-a"].ToInt64() == IncrementsPerNamespace && _["__lastHandled"]["namespace-b"].ToInt64() == IncrementsPerNamespace);
    [Fact] void should_stamp_the_legacy_document_with_an_incarnation() => _documents[0].Contains("__incarnation").ShouldBeTrue();
    [Fact] void should_not_write_the_shared_scalar_watermark() => _documents.ShouldEachConformTo(_ => !_.Contains("__lastHandledEventSequenceNumber"));
    [Fact] void should_not_create_duplicate_documents() => _documentCount.ShouldEqual(Counters);
}
