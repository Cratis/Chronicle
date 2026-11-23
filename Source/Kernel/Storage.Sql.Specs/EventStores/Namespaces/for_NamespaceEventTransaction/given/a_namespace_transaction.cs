// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Occurrences;
using Cratis.Chronicle.Storage.Sql.for_Database.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.for_NamespaceEventTransaction.given;

public class a_namespace_transaction : a_sqlite_database
{
    protected readonly NamespaceIncarnationId _incarnation = NamespaceIncarnationId.New();
    protected NamespaceMarkerStorage _markers;
    protected NamespaceCommitContext _context;
    protected bool _called;
    private protected NamespaceEventTransaction<EventSequenceDbContext> _transaction;

    void Establish()
    {
        _markers = new(() => _database.Namespace("fenced-store", "fenced-namespace"));
        _transaction = new(() => _database.EventSequenceTable("fenced-store", "fenced-namespace", "events"));
        _context = new(_incarnation, CancellationToken.None) { ActivationIncarnation = _incarnation };
    }

    protected async Task<int> WriteReceipt(EventSequenceDbContext database, CancellationToken cancellationToken)
    {
        _called = true;
        database.Set<EffectReceiptEntry>().Add(new() { Id = "receipt", OccurrenceId = "occurrence", Data = "{}" });
        await database.SaveChangesAsync(cancellationToken);
        return 42;
    }

    protected async Task<bool> HasReceipt()
    {
        await using var scope = await _database.Namespace("fenced-store", "fenced-namespace");
        return await scope.DbContext.Set<EffectReceiptEntry>().AnyAsync(row => row.Id == "receipt");
    }
}
