// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class a_receipt_storage : a_publication_storage
{
    static readonly ServiceProvider _services = new ServiceCollection().BuildServiceProvider();
    protected readonly string _receiptId = OccurrenceIdentities.ForEffect(new OccurrenceId(Guid.NewGuid().ToString("N")), "effect");
    string? _directory;

    protected EffectReceipt Receipt(ulong? number) => new(_receiptId, new LeaseToken("occurrence", 1), number.HasValue ? EffectOutcome.Appended : EffectOutcome.NoEvents, number.HasValue ? _eventSequenceId : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, [], "{\"winner\":1}", DateTimeOffset.UnixEpoch);

    async Task Establish()
    {
        _namespace = $"receipts-{Guid.NewGuid():N}";
        var connectionString = _connectionString;
        if (Environment.GetEnvironmentVariable("CHRONICLE_SQL_SPECS_PROVIDER") is null)
        {
            _directory = Path.GetFullPath(Path.Combine(".ai-work", "4911", $"sql-receipts-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(_directory);
            connectionString = $"Data Source={Path.Combine(_directory, "cluster.db")}";
        }

        _database = new Database(_services, Options.Create(new ChronicleOptions { Storage = new() { ConnectionDetails = connectionString } }), _migrator, Substitute.For<IUniqueConstraintMigrator>(), Substitute.For<IReadModelMigrator>());

        // Provision only the event table; receipt operations must initialize namespace storage themselves.
        await using var sequence = await _database.EventSequenceTable(_eventStore, _namespace, _tableName);
        _connectionString = sequence.DbContext.Database.GetConnectionString()!;
        _storage = RecreateStorage();
    }

    void Destroy()
    {
        if (_directory is not null)
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    protected IEffectReceiptStorage Recreate() => RecreateStorage();
    protected EventToAppendToStorage EventAt(EventSequenceNumber number) => _event with { SequenceNumber = number };
}
