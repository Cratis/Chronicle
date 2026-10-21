// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

public sealed class CommitBeforeFailure : DbTransactionInterceptor
{
    public int CommitCount { get; private set; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        CommitCount++;
        if (CommitCount == 1)
        {
            throw new TimeoutException("Simulated failure before commit.");
        }

        return ValueTask.FromResult(result);
    }
}
