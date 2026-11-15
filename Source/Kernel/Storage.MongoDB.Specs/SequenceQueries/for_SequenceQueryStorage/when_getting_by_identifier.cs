// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage;

[Collection(MongoDBCollection.Name)]
public class when_getting_by_identifier(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    Concepts.SequenceQueries.SequenceQueryDefinition? _query;
    Concepts.SequenceQueries.SequenceQueryDefinition? _otherCaseQuery;
    Concepts.SequenceQueries.SequenceQueryDefinition? _missingQuery;
    SequenceQueryFolderDefinition? _folder;
    SequenceQueryFolderDefinition? _missingFolder;

    async Task Because()
    {
        var storage = new SequenceQueryStorage(new NamespaceBackedDatabase(_database));
        await storage.Insert(new Concepts.SequenceQueries.SequenceQueryDefinition(
            "query-1",
            "Stored",
            SequenceQueryScope.Everyone,
            "alice",
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false));
        await storage.InsertFolder(new SequenceQueryFolderDefinition("folder-1", SequenceQueryScope.User, "alice", "default", "stored"));

        _query = await storage.Get("query-1");
        _otherCaseQuery = await storage.Get("QUERY-1");
        _missingQuery = await storage.Get("query-2");
        _folder = await storage.GetFolder("folder-1");
        _missingFolder = await storage.GetFolder("folder-2");
    }

    [Fact] void should_get_the_query_with_its_stored_owner() => _query!.Owner.Value.ShouldEqual("alice");
    [Fact] void should_not_find_the_query_under_another_spelling_of_the_identifier() => _otherCaseQuery.ShouldBeNull();
    [Fact] void should_get_nothing_for_a_missing_query() => _missingQuery.ShouldBeNull();
    [Fact] void should_get_the_folder_with_its_stored_owner() => _folder!.Owner.Value.ShouldEqual("alice");
    [Fact] void should_get_nothing_for_a_missing_folder() => _missingFolder.ShouldBeNull();

    sealed class NamespaceBackedDatabase(IEventStoreNamespaceDatabase database) : IEventStoreDatabase
    {
        public IMongoCollection<T> GetCollection<T>(string? collectionName = null) => database.GetCollection<T>(collectionName);

        public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace) => database;
    }
}
