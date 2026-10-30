// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Commits a competing revision after the revise's read but before its first update executes, then counts the updates.
/// </summary>
/// <param name="competingWrite">The competing committed write, run once.</param>
public class CompetingRevision(Func<Task> competingWrite) : DbCommandInterceptor
{
    bool _competed;

    /// <summary>
    /// Gets the number of update commands the revise executed, excluding the competing writer's.
    /// </summary>
    public int UpdatesExecuted { get; private set; }

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_competed)
        {
            _competed = true;
            await competingWrite();
        }

        UpdatesExecuted++;
        return result;
    }
}
