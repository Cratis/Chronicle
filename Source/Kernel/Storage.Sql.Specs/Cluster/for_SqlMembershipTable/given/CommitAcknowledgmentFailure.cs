// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

public sealed class CommitAcknowledgmentFailure : DbTransactionInterceptor
{
    public int CommitCount { get; private set; }

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        CommitCount++;
        if (CommitCount == 1)
        {
            throw new TimeoutException("Simulated lost commit acknowledgment.");
        }

        return Task.CompletedTask;
    }
}
