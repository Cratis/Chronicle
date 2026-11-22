// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public sealed class a_forced_unique_failure(Func<Task> commitWinner) : SaveChangesInterceptor
{
    bool _fired;
    DbContext? _batchContext;
    public int UniqueFailures { get; private set; }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (!_fired)
        {
            _fired = true;
            _batchContext = eventData.Context;

            // Deliberately end only the fixture's read-only snapshot to force a stale ownership read.
            // This proves real unique-error recovery, not a naturally occurring serializable race.
            var transaction = eventData.Context!.Database.CurrentTransaction!;
            await transaction.CommitAsync(cancellationToken);
            await transaction.DisposeAsync();
            await commitWinner();
        }
        return result;
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!ReferenceEquals(eventData.Context, _batchContext)) return Task.CompletedTask;
        if (eventData.Exception is DbUpdateException error && error.IsUniqueViolation()) UniqueFailures++;
        return Task.CompletedTask;
    }
}
