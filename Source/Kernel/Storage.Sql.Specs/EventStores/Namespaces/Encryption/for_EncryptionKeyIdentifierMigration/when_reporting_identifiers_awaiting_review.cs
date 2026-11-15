// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyIdentifierMigration;

[Collection(SqlServerCollection.Name)]
public class when_reporting_identifiers_awaiting_review(SqlServerFixture fixture) : for_EncryptionKeyStorage.given.a_key_store(new("sql_server", fixture.CreateDatabase, fixture.DropDatabase))
{
    sealed class Lines : ILoggerProvider, ILogger
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(logLevel + ":" + formatter(state, exception));

        public void Dispose()
        {
        }
    }

    Lines _lines;
    ServiceProvider _services;
    int _afterFirst;
    int _afterSecond;

    async Task Because()
    {
        _lines = new();
        _services = new ServiceCollection().AddLogging(_ => _.AddProvider(_lines)).BuildServiceProvider();
        var options = new ChronicleOptions { Storage = new() { Type = StorageType.MsSql, ConnectionDetails = "Server=localhost;TrustServerCertificate=True" } };
        var database = new Database(_services, Options.Create(options), Substitute.For<IEventSequenceMigrator>(), Substitute.For<IUniqueConstraintMigrator>(), Substitute.For<IReadModelMigrator>());
        await _harness.UpgradeFromEarlierLayout("INSERT INTO [EncryptionKeys] ([Identifier], [Revision], [PublicKey], [PrivateKey]) VALUES (N'Alice', 1, 0x0A, 0x0B), (N'alice', 2, 0x0A, 0x0B)");
        await using var context = _harness.Context();
        var target = new SqlTarget("Server=one", "dbo", null);
        await database.ReportIdentifiersAwaitingReview(context, target);
        _afterFirst = _lines.Messages.Count;
        await database.ReportIdentifiersAwaitingReview(context, target);
        _afterSecond = _lines.Messages.Count;
    }

    [Fact] void should_log_one_warning() => _afterFirst.ShouldEqual(1);
    [Fact] void should_log_the_number_of_rows() => _lines.Messages[0].ShouldContain("2 rows");
    [Fact] void should_not_log_an_identifier() => _lines.Messages.Exists(_ => _.Contains("alice", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_log_once_per_target() => _afterSecond.ShouldEqual(_afterFirst);
}
