// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public sealed class a_late_batch_failure(Exception? error, Action? beforeFailure = null) : SaveChangesInterceptor
{
    public int Attempts { get; private set; }
    public int SuccessfulChunks { get; private set; }
    public Exception? Failure { get; private set; }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (++Attempts == 1)
        {
            beforeFailure?.Invoke();
            if (error is not null) throw error;

            // Send an invalid value to the real provider, rather than synthesizing a database failure.
            var entry = eventData.Context!.ChangeTracker.Entries().First(entry => entry.State == EntityState.Added);
            var identity = entry.Metadata.FindPrimaryKey()!.Properties.Single().Name;
            entry.Property(identity == "Value" ? "EventSourceId" : "Value").CurrentValue = null;
        }
        return ValueTask.FromResult(result);
    }

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        SuccessfulChunks++;
        return ValueTask.FromResult(result);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        Failure = eventData.Exception;
        return Task.CompletedTask;
    }
}
