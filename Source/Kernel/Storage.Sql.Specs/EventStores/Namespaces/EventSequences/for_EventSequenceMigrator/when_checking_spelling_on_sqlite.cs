// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

public class when_checking_spelling_on_sqlite : Specification
{
    SqliteConnection _connection;
    string _path;
    bool _reviewTableExists;
    given.a_captured_migrator_log _log;

    void Establish()
    {
        _path = Path.Combine(Path.GetTempPath(), $"chronicle-spelling-{Guid.NewGuid():N}.db");
        _connection = new($"DataSource={_path};Pooling=False");
    }

    async Task Because()
    {
        _log = new();
        var migrator = new EventSequenceMigrator(new TableMigrator<EventSequenceDbContext>(NullLogger<TableMigrator<EventSequenceDbContext>>.Instance), _log);
        var builder = new DbContextOptionsBuilder<EventSequenceDbContext>().UseSqlite(_connection.ConnectionString);
        builder.AddConceptAsSupport().WithoutServiceProviderCaching();
        await using var context = new EventSequenceDbContext(builder.Options, "event-sequence", migrator);
        await context.EnsureTableExists();
        await migrator.CheckSpelling(context, "event-sequence");
        await _connection.OpenAsync();
        await using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM sqlite_master WHERE name = '{EventSequenceMigrator.SpellingReviewsTable}'";
        _reviewTableExists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    void Destroy()
    {
        _connection.Dispose();
        File.Delete(_path);
    }

    [Fact] void should_not_create_the_review_table() => _reviewTableExists.ShouldBeFalse();
    [Fact] void should_not_log() => _log.Messages.Where(_ => _.Level >= Microsoft.Extensions.Logging.LogLevel.Information).ShouldBeEmpty();
}
