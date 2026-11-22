// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.when_applying_changes;

[Collection(SqlServerCollection.Name)]
public class and_the_provider_is_sql_server(SqlServerFixture fixture) : given.batch_parity_scenarios
{
    async Task Establish()
    {
        var connection = await fixture.CreateDatabase();
        Configure(builder => builder.UseSqlServer(connection), true, () => fixture.QueueDatabaseDrop(connection));
    }

    Task Because() => CheckParity();

    [Fact] void should_match_sequential_checks() => VerifyClaims();
    [Fact] void should_match_sequential_refusals_and_complete_rows() => VerifyChanges();
}
