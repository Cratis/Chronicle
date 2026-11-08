// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SequenceQueries;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage.given;

/// <summary>
/// A storage that already holds one query and one folder, both stored under the owner alice.
/// </summary>
/// <param name="fixture">The MongoDB fixture.</param>
public abstract class a_storage_holding_one_query_and_one_folder(MongoDBFixture fixture) : Indexing.given.a_real_namespace_database(fixture)
{
    protected ISequenceQueryStorage _storage;

    protected static Concepts.SequenceQueries.SequenceQueryDefinition Query(string id, string name, string owner) =>
        new(
            id,
            name,
            SequenceQueryScope.User,
            owner,
            string.Empty,
            "default",
            "event-log",
            Concepts.SequenceQueries.SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);

    protected static SequenceQueryFolderDefinition Folder(string id, string owner, string path) =>
        new(id, SequenceQueryScope.User, owner, "default", path);

    async Task Establish()
    {
        _storage = new SequenceQueryStorage(new NamespaceBackedDatabase(_database));
        await _storage.Insert(Query("query-1", "Stored", "alice"));
        await _storage.InsertFolder(Folder("folder-1", "alice", "stored"));
    }

    sealed class NamespaceBackedDatabase(IEventStoreNamespaceDatabase database) : IEventStoreDatabase
    {
        public IMongoCollection<T> GetCollection<T>(string? collectionName = null) => database.GetCollection<T>(collectionName);

        public IEventStoreNamespaceDatabase GetNamespaceDatabase(EventStoreNamespaceName @namespace) => database;
    }
}
