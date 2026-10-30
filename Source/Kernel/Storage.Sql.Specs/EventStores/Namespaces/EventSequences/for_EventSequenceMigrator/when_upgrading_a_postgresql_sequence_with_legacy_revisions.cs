// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_a_postgresql_sequence_with_legacy_revisions(PostgreSqlFixture fixture) : when_upgrading_with_legacy_revisions
{
    protected override Task<string> CreateDatabase() => fixture.CreateDatabase();

    protected override Task DropDatabase(string connectionString) => fixture.DropDatabase(connectionString);

    protected override void UseProvider(DbContextOptionsBuilder<EventSequenceDbContext> builder, string connectionString) =>
        builder.UseNpgsql(connectionString);
}
