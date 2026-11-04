// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_SqlServerDatabaseCreation.when_enabling_read_committed_snapshot;

[Collection(SqlServerCollection.Name)]
public class and_another_session_is_idle_in_the_database(SqlServerFixture fixture) : Specification
{
    string _connectionString;
    SqlConnection _holder;
    RecordingLogger _logger;
    int _snapshotOn;
    int _holderSession;

    async Task Establish()
    {
        _connectionString = await fixture.CreateDatabase();
        _logger = new();
        _holder = new SqlConnection(SqlServerSpecs.Unpooled(_connectionString, application: "spec-holder"));
        await _holder.OpenAsync();
        await using var command = _holder.CreateCommand();
        command.CommandText = "SELECT @@SPID";
        _holderSession = Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    async Task Because()
    {
        await SqlServerReadCommittedSnapshot.Enable(_connectionString, null, _logger, TimeSpan.FromSeconds(0.5), 10);
        _snapshotOn = await SqlServerSpecs.SnapshotOn(_connectionString);
    }

    async Task Destroy()
    {
        await _holder.DisposeAsync();
        await fixture.DropDatabase(_connectionString);
    }

    [Fact] void should_enable_read_committed_snapshot() => _snapshotOn.ShouldEqual(1);
    [Fact] void should_report_the_blocking_session() => _logger.Messages.ShouldContain(_ => _.Contains($"session {_holderSession}"));
    [Fact] void should_not_report_the_blocking_program() => _logger.Messages.ShouldNotContain(_ => _.Contains("spec-holder"));

    sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
