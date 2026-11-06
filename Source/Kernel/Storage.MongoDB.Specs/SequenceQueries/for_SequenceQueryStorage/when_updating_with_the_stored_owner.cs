// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage.given;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage;

[Collection(MongoDBCollection.Name)]
public class when_updating_with_the_stored_owner(MongoDBFixture fixture) : given.a_storage_holding_one_query_and_one_folder(fixture)
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    Concepts.SequenceQueries.SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;

    async Task Because()
    {
        _query = await _storage.Update(Query("query-1", "Changed", "bob"), "alice");
        _folder = await _storage.UpdateFolder(Folder("folder-1", "bob", "changed"), "alice");
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_apply_the_query() => _query.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_apply_the_folder() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Applied);
    [Fact] void should_change_the_query_and_keep_its_owner() => _storedQuery!.ShouldBeStoredAs("Changed", "alice");
    [Fact] void should_change_the_folder_and_keep_its_owner() => _storedFolder!.ShouldBeStoredAs("alice", "changed");
}
