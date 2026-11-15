// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage.when_owners_differ_only_in_spelling;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_trailing_space(SqlServerFixture fixture) : given.two_owner_spellings(fixture, "alice", "alice ")
{
    Task Because() => SaveAndRead();

    [Fact] void should_be_collapsed_by_the_collation_of_the_database() => _collapsedByCollation.ShouldEqual(1);
    [Fact] void should_return_the_own_and_shared_queries_only() => string.Join(',', _queriesForFirst).ShouldEqual("query-own,query-shared");
    [Fact] void should_return_the_own_and_shared_folders_only() => string.Join(',', _foldersForFirst).ShouldEqual("folder-own,folder-shared");
}
