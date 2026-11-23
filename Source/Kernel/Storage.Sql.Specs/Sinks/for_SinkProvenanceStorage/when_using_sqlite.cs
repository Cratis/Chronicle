// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_using_sqlite : given.a_provenance_database
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-provenance-{Guid.NewGuid():N}");
    Exception? _writeError;

    async Task Because()
    {
        Directory.CreateDirectory(_directory);
        var connection = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "primary.db") };
        var independent = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "independent.db") };
        var alias = new SqliteConnectionStringBuilder(connection.ConnectionString) { Pooling = false };
        await Exercise(connection.ConnectionString, independent.ConnectionString, alias.ConnectionString);
        await new SinkProvenanceStorage(DatabaseFor(independent.ConnectionString), "store", "namespace").IsQuarantined();
        var readOnly = new SqliteConnectionStringBuilder(independent.ConnectionString) { Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        _writeError = await Catch.Exception(() => new SinkProvenanceStorage(DatabaseFor(readOnly.ConnectionString, SchemaMode.Verify), "store", "namespace").MarkQuarantined());
    }

    [Fact] void should_fail_closed_when_the_durable_mark_cannot_be_written() => _writeError.ShouldBeOfExactType<SqliteException>();

    void Destroy()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }
}
