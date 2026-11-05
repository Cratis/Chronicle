// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.InMemory.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.for_SequenceQueryStorage;

public class when_getting_by_identifier : Specification
{
    SequenceQueryDefinition? _query;
    SequenceQueryDefinition? _missingQuery;
    SequenceQueryFolderDefinition? _folder;
    SequenceQueryFolderDefinition? _missingFolder;

    async Task Because()
    {
        using var storage = new SequenceQueryStorage();
        await storage.Save(new SequenceQueryDefinition(
            "query-1",
            "Stored",
            SequenceQueryScope.Everyone,
            "alice",
            string.Empty,
            "default",
            "event-log",
            SequenceQueryFilter.Empty,
            SequenceQuerySortBy.Occurred,
            Descending: false));
        await storage.SaveFolder(new SequenceQueryFolderDefinition("folder-1", SequenceQueryScope.User, "alice", "default", "stored"));

        _query = await storage.Get("query-1");
        _missingQuery = await storage.Get("query-2");
        _folder = await storage.GetFolder("folder-1");
        _missingFolder = await storage.GetFolder("folder-2");
    }

    [Fact] void should_get_the_query_with_its_stored_owner() => _query!.Owner.Value.ShouldEqual("alice");
    [Fact] void should_get_nothing_for_a_missing_query() => _missingQuery.ShouldBeNull();
    [Fact] void should_get_the_folder_with_its_stored_owner() => _folder!.Owner.Value.ShouldEqual("alice");
    [Fact] void should_get_nothing_for_a_missing_folder() => _missingFolder.ShouldBeNull();
}
