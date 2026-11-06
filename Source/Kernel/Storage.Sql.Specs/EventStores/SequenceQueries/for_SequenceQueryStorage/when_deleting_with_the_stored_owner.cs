// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage;

public class when_deleting_with_the_stored_owner : given.a_storage_holding_one_query_and_one_folder
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    Concepts.SequenceQueries.SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;

    async Task Because()
    {
        _query = await _storage.Delete("query-1", "alice");
        _folder = await _storage.DeleteFolder("folder-1", "alice");
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_apply_the_query() => _query.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_apply_the_folder() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_delete_the_query() => _storedQuery.ShouldBeNull();
    [Fact] void should_delete_the_folder() => _storedFolder.ShouldBeNull();
}
