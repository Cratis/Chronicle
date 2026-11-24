// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2;

[Collection(SqlServerCollection.Name)]
public class when_running_the_sql_server_contract(SqlServerFixture fixture) : UniqueConstraintsStorageV2SqlContract
{
    [Fact]
    public async Task should_refuse_a_padding_colliding_owner_key_without_replacing_the_original_claim()
    {
        await Seed();
        ShouldRefuse(await _storage.Save("owner ", _definition, 99, _otherValue, _scope, _authority), UniqueConstraintWriteFailure.ConflictingClaim);
        await ShouldRetainClaim();
        (await _storage.GetFor("owner ", _definition, _scope)).ShouldBeEmpty();
    }

    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, string connectionString) => builder.UseSqlServer(connectionString);

    protected override Task ReleaseDatabase() => fixture.DropDatabase(_connectionString);
}
