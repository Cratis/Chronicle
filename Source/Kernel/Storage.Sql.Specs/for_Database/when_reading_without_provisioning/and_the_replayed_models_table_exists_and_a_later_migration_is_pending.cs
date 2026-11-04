// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_reading_without_provisioning;

/// <summary>
/// A namespace that has the table of replayed read models but still has a later migration to apply is not an absent namespace:
/// the occurrences it holds are the allow-list reads are checked against.
/// </summary>
public class and_the_replayed_models_table_exists_and_a_later_migration_is_pending : given.a_database_without_read_model_storage
{
    static readonly DateTimeOffset _started = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    ReplayedModelsStorage _storage;
    ReadModelOccurrence _occurrence;
    int _pendingMigrations;
    ReadModelOccurrence[] _occurrences;

    async Task Establish()
    {
        _storage = new ReplayedModelsStorage("store", "namespace", _database);
        var readModel = new ReadModelType(new ReadModelIdentifier("some-read-model"), ReadModelGeneration.First);
        _occurrence = new("some-observer", _started, readModel, "some-read-model", "some-read-model-20260101120000");
        await _storage.Replayed(_occurrence);

        string connectionString;
        string applied;
        await using (var scope = await _database.Namespace("store", "namespace"))
        {
            connectionString = new SqliteConnectionStringBuilder { DataSource = scope.DbContext.Database.GetDbConnection().DataSource, Pooling = false }.ConnectionString;
            applied = (await scope.DbContext.Database.GetAppliedMigrationsAsync()).Last();
        }

        await using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = @id";
            command.Parameters.AddWithValue("@id", applied);
            await command.ExecuteNonQueryAsync();
        }

        await using var context = Context(connectionString);
        _pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).Count();
    }

    async Task Because() => _occurrences = (await _storage.GetOccurrences(_occurrence.Type.Identifier)).ToArray();

    [Fact] void should_have_left_a_migration_pending_in_the_setup() => _pendingMigrations.ShouldEqual(1);
    [Fact] void should_still_know_the_occurrence() => _occurrences.Length.ShouldEqual(1);
}
