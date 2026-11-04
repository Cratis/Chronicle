// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// An occurrence table an older kernel left in PostgreSQL, read through a sink on the real <see cref="Database"/>.
/// </summary>
/// <param name="fixture">The <see cref="PostgreSqlFixture"/>.</param>
public abstract class an_older_postgresql_occurrence(PostgreSqlFixture fixture) : a_postgresql_database_with_connection_resolution(fixture)
{
    protected OccurrenceRead _read;
    protected string _catalogBefore;
    protected string _catalogAfter;
    protected string[] _databasesBefore;
    protected string[] _databasesAfter;
    protected long _rowsBefore;

    protected abstract Database Reader { get; }

    async Task Establish()
    {
        await ProvisionPrimaryTable();
        var connection = await ReadModelConnection();
        await CreateOlderOccurrenceTable(connection, ColdIndexedSink.Occurrence);
        _rowsBefore = await Count(connection, $"SELECT COUNT(*) FROM \"{ColdIndexedSink.Occurrence}\"");
        _catalogBefore = await Catalog(connection);
        _databasesBefore = await PhysicalDatabases();
    }
}
