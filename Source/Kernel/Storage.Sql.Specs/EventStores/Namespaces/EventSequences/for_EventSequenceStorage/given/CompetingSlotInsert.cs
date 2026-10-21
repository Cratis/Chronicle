// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

/// <summary>
/// Commits the competing writer's slot after the append's read but before its insert.
/// </summary>
/// <param name="competingWrite">The competing committed write.</param>
public class CompetingSlotInsert(Action competingWrite) : SaveChangesInterceptor
{
    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        competingWrite();
        return ValueTask.FromResult(result);
    }
}
