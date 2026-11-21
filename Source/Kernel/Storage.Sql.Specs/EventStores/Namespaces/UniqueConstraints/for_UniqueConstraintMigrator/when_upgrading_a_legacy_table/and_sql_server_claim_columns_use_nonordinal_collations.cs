// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintMigrator.when_upgrading_a_legacy_table;

[Collection(SqlServerCollection.Name)]
public class and_sql_server_claim_columns_use_nonordinal_collations(SqlServerFixture fixture) : given.an_ordinal_claim_upgrade
{
    async Task Establish()
    {
        var connection = await fixture.CreateDatabase();
        Configure(connection, () => fixture.QueueDatabaseDrop(connection));
    }

    Task Because() => CheckUpgrades();

    [Fact] void should_preserve_rows_column_shapes_and_index_definitions() => VerifyPreservation();
    [Fact] void should_make_new_and_legacy_identity_columns_ordinal() => VerifyCollations();
    [Fact] void should_fail_closed_and_retry_unsupported_dependencies() => VerifyFailures();
}
