// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class a_receipt_storage : a_publication_storage
{
    protected readonly string _receiptId = OccurrenceIdentities.ForEffect(new OccurrenceId(Guid.NewGuid().ToString("N")), "effect");

    protected EffectReceipt Receipt(ulong? number) => new(_receiptId, new LeaseToken("occurrence", 1), number.HasValue ? EffectOutcome.Appended : EffectOutcome.NoEvents, number.HasValue ? _eventSequenceIdForReceipt : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, number.HasValue ? new EventSequenceNumber(number.Value) : null, [], "{\"winner\":1}", DateTimeOffset.UnixEpoch);

    Cratis.Chronicle.Concepts.EventSequences.EventSequenceId _eventSequenceIdForReceipt => _eventSequenceId;
    async Task Establish()
    {
        await using var sequence = CreateContext();
        var builder = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<NamespaceDbContext>();
        if (sequence.Database.IsNpgsql())
        {
            builder.UseNpgsql(_connectionString);
        }
        else if (sequence.Database.IsSqlServer())
        {
            builder.UseSqlServer(_connectionString);
        }
        else
        {
            builder.UseSqlite(_connectionString);
        }

        builder.AddConceptAsSupport();
        await using var context = new NamespaceDbContext(builder.Options);
        await context.Database.MigrateAsync();
    }
    protected IEffectReceiptStorage Recreate() => RecreateStorage();
    protected EventToAppendToStorage EventAt(EventSequenceNumber number) => _event with { SequenceNumber = number };
}
