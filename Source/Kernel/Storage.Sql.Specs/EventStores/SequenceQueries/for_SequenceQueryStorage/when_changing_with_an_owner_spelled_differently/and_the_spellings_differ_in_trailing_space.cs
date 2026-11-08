// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SequenceQueries;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.for_SequenceQueryStorage.when_changing_with_an_owner_spelled_differently;

[Collection(SqlServerCollection.Name)]
public class and_the_spellings_differ_in_trailing_space(SqlServerFixture fixture) : given.a_stored_owner_and_a_differently_spelled_one(fixture, "alice", "alice ")
{
    Task Because() => ChangeAndRead();

    [Fact] void should_refuse_the_query_update() => _updatedQuery.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_folder_update() => _updatedFolder.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_query_delete() => _deletedQuery.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_refuse_the_folder_delete() => _deletedFolder.ShouldEqual(SequenceQueryWriteOutcome.Conflict);
    [Fact] void should_keep_the_query_as_stored() => _query!.Name.Value.ShouldEqual("Stored");
    [Fact] void should_keep_the_folder_as_stored() => _folder!.Path.Value.ShouldEqual("stored");
}
