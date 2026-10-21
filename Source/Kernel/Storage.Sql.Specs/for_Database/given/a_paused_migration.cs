// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public sealed class a_paused_migration : DbConnectionInterceptor
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _paused;

    public Task Entered => _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

    public void Release() => _release.TrySetResult();

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        // Pause before SQLite acquires its own writer lock, isolating Database's coordination.
        if (Interlocked.Exchange(ref _paused, 1) == 0)
        {
            _entered.TrySetResult();
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }

        return result;
    }
}
