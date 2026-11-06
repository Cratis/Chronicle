// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage.given;

/// <summary>
/// A storage that already holds one query and one folder, both stored under the owner alice.
/// </summary>
public abstract class a_storage_holding_one_query_and_one_folder : Specification
{
    protected ISequenceQueryStorage _storage;

    protected static SequenceQueryDefinition Query(string id, string name, string owner) =>
        new(
            id,
            name,
            SequenceQueryScope.User,
            owner,
            string.Empty,
            "default",
            "event-log",
            SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false);

    protected static SequenceQueryFolderDefinition Folder(string id, string owner, string path) =>
        new(id, SequenceQueryScope.User, owner, "default", path);

    async Task Establish()
    {
        _storage = new SequenceQueryStorage();
        await _storage.Insert(Query("query-1", "Stored", "alice"));
        await _storage.InsertFolder(Folder("folder-1", "alice", "stored"));
    }

    void Destroy() => ((SequenceQueryStorage)_storage).Dispose();
}
