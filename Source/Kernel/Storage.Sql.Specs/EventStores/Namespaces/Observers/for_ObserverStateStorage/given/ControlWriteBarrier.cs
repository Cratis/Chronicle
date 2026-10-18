// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.given;

/// <summary>
/// Suspends a state save after it has read the original control and before its update reaches SQL.
/// </summary>
public sealed class ControlWriteBarrier : SaveChangesInterceptor
{
    /// <summary>
    /// Gets the signal that the original control has been read.
    /// </summary>
    public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets the signal releasing the stale write after a competing save completes.
    /// </summary>
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <inheritdoc/>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Reached.TrySetResult();
        await Continue.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        return result;
    }
}
