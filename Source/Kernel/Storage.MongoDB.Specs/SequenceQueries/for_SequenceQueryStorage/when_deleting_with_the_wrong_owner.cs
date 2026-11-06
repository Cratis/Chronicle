// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage.given;
using Cratis.Chronicle.Storage.MongoDB.Sinks;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.MongoDB.SequenceQueries.for_SequenceQueryStorage;

[Collection(MongoDBCollection.Name)]
public class when_deleting_with_the_wrong_owner(MongoDBFixture fixture) : given.a_storage_holding_one_query_and_one_folder(fixture)
{
    SequenceQueryWriteOutcome _query;
    SequenceQueryWriteOutcome _folder;
    Concepts.SequenceQueries.SequenceQueryDefinition? _storedQuery;
    SequenceQueryFolderDefinition? _storedFolder;

    async Task Because()
    {
        _query = await _storage.Delete("query-1", "bob");
        _folder = await _storage.DeleteFolder("folder-1", "bob");
        _storedQuery = await _storage.Get("query-1");
        _storedFolder = await _storage.GetFolder("folder-1");
    }

    [Fact] void should_refuse_the_query() => _query.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_folder() => _folder.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_keep_the_query() => _storedQuery!.ShouldBeStoredAs("Stored", "alice");
    [Fact] void should_keep_the_folder() => _storedFolder!.ShouldBeStoredAs("alice", "stored");
}
