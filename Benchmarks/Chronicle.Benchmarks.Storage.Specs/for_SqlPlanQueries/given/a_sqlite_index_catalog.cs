// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Benchmarks.Storage.for_SqlPlanQueries.given;

public class a_sqlite_index_catalog : Specification
{
    SqliteConnection _connection = default!;
    private protected SqlPlanQueries _queries = default!;
    protected string _indexes = default!;

    async Task Establish()
    {
        var connectionString = $"DataSource=plans_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _connection = new(connectionString);
        await _connection.OpenAsync();
        await using var command = _connection.CreateCommand();
        command.CommandText = "CREATE TABLE events (sequence_number INTEGER); CREATE INDEX events_sequence_number ON events (sequence_number)";
        await command.ExecuteNonQueryAsync();
        _queries = new(StorageProvider.Sqlite, connectionString);
    }

    async Task Destroy() => await _connection.DisposeAsync();
}
