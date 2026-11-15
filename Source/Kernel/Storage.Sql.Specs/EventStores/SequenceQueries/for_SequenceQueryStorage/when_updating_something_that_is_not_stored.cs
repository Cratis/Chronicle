// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage;

public class when_updating_something_that_is_not_stored : given.a_storage_holding_one_query_and_one_folder
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    Concepts.SequenceQueries.SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;
    Concepts.SequenceQueries.SequenceQueryDefinition? _missingQuery;
    SequenceQueryFolderDefinition? _missingFolder;

    async Task Because()
    {
        _query = await _storage.Update(Query("query-2", "Changed", "alice"), "alice");
        _folder = await _storage.UpdateFolder(Folder("folder-2", "alice", "changed"), "alice");
        _missingQuery = await _storage.Get("query-2");
        _missingFolder = await _storage.GetFolder("folder-2");
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_refuse_the_query() => _query.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_folder() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_not_store_a_query() => _missingQuery.ShouldBeNull();
    [Fact] void should_not_store_a_folder() => _missingFolder.ShouldBeNull();
}
