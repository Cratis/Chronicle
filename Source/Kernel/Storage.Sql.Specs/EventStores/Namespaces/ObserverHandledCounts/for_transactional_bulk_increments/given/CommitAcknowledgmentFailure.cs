// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverHandledCounts.for_transactional_bulk_increments.given;

public sealed class CommitAcknowledgmentFailure : DbTransactionInterceptor
{
    public bool Enabled { get; set; }
    public int Attempts { get; private set; }

    public void Reset()
    {
        Enabled = false;
        Attempts = 0;
    }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        Attempts++;
        if (Enabled) throw new TimeoutException("Lost acknowledgment after real commit.");

        return Task.CompletedTask;
    }
}
