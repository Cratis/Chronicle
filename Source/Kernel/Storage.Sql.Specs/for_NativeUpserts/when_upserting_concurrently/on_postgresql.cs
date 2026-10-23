// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_NativeUpserts.when_upserting_concurrently;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    (int Reactors, int Tokens, int Partitions, bool ReactorPayload, bool TokenPayload, bool PartitionPayload) _saved;

    async Task Because() => _saved = await given.concurrent_writes.Run(Database);

    [Fact] void should_keep_one_reactor() => _saved.Reactors.ShouldEqual(1);
    [Fact] void should_keep_one_token() => _saved.Tokens.ShouldEqual(1);
    [Fact] void should_keep_one_failed_partition() => _saved.Partitions.ShouldEqual(1);
    [Fact] void should_keep_a_reactor_writer_payload() => _saved.ReactorPayload.ShouldBeTrue();
    [Fact] void should_keep_a_token_writer_payload() => _saved.TokenPayload.ShouldBeTrue();
    [Fact] void should_keep_a_partition_writer_payload() => _saved.PartitionPayload.ShouldBeTrue();
}
