// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage.given;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage;

public class when_inserting_an_existing_identifier : given.a_storage_holding_one_query_and_one_folder
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    SequenceQueryWriteOutcome _newQuery;
    SequenceQueryWriteOutcome _newFolder;
    SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;

    async Task Because()
    {
        _query = await _storage.Insert(Query("query-1", "Second", "bob"));
        _folder = await _storage.InsertFolder(Folder("folder-1", "bob", "second"));
        _newQuery = await _storage.Insert(Query("query-2", "New", "bob"));
        _newFolder = await _storage.InsertFolder(Folder("folder-2", "bob", "new"));
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_refuse_the_query() => _query.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_folder() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_leave_the_stored_query_unchanged() => _storedQuery!.ShouldBeStoredAs("Stored", "alice");
    [Fact] void should_leave_the_stored_folder_unchanged() => _storedFolder!.ShouldBeStoredAs("alice", "stored");
    [Fact] void should_insert_a_new_query() => _newQuery.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_insert_a_new_folder() => _newFolder.ShouldEqual(SequenceQueryWriteOutcome.Applied);
}
