// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage.given;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage;

public class when_deleting_something_that_is_not_stored : given.a_storage_holding_one_query_and_one_folder
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;

    async Task Because()
    {
        _query = await _storage.Delete("query-2", "alice");
        _folder = await _storage.DeleteFolder("folder-2", "alice");
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_treat_the_query_as_deleted() => _query.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_treat_the_folder_as_deleted() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_leave_the_stored_query() => _storedQuery!.ShouldBeStoredAs("Stored", "alice");
    [Fact] void should_leave_the_stored_folder() => _storedFolder!.ShouldBeStoredAs("alice", "stored");
}
