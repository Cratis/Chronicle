// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

/// <summary>
/// An occurrence table an older kernel left in SQLite, read through a sink on the real <see cref="Database"/>.
/// </summary>
public abstract class an_older_sqlite_occurrence : a_database_without_read_model_storage
{
    protected OccurrenceRead _read;
    protected string _catalogBefore;
    protected string _catalogAfter;
    protected string _filesBefore;
    protected string _filesAfter;
    protected string _readModelConnection;

    /// <summary>
    /// Gets the JSON schema of the read model the sink reads.
    /// </summary>
    protected abstract string Schema { get; }

    /// <summary>
    /// Gets the SQL that creates the older table, which holds one row.
    /// </summary>
    protected abstract string OlderTableSql { get; }

    protected abstract Database Reader { get; }

    async Task Establish()
    {
        _readModelConnection = await ProvisionReadModelDatabase();
        await Execute(_readModelConnection, OlderTableSql);
        await Execute(_readModelConnection, $"INSERT INTO \"{ColdIndexedSink.Occurrence}\" (\"id\") VALUES ('1')");
        _catalogBefore = await Catalog(_readModelConnection);
        _filesBefore = string.Join(',', FilesOnDisk);
    }
}
