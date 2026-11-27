// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_reading_after_restricted_inventory_is_lost : given.a_provenance_database
{
    readonly string _directory = Path.Combine(Path.GetTempPath(), $"cratis-provenance-{Guid.NewGuid():N}");
    string _connectionString;
    SinkProvenanceState _state;

    async Task Establish()
    {
        Directory.CreateDirectory(_directory);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(_directory, "primary.db"), Pooling = false }.ConnectionString;
        await new SinkProvenanceStorage(DatabaseFor(_connectionString), "store", "namespace").CompleteInventory(true);
        await Execute(_connectionString, "DELETE FROM \"__cratis_sink_inventory\" WHERE \"Id\" = 'inventory'");
    }

    async Task Because() => _state = await new SinkProvenanceStorage(DatabaseFor(_connectionString), "store", "namespace").ReadState();

    [Fact] void should_not_recover_completion() => _state.IsComplete.ShouldBeFalse();
    [Fact] void should_recover_restriction() => _state.Restricted.ShouldBeTrue();

    void Destroy() => Directory.Delete(_directory, true);
}
