// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

public class OnlineIndexGate : DbCommandInterceptor
{
    int _paused;
    bool _onlineRequested;
    public bool ForceOffline { get; set; }
    public bool UsedOfflineBuild { get; private set; }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("SERVERPROPERTY('EngineEdition')", StringComparison.Ordinal)) _onlineRequested = true;
#pragma warning disable CA2100 // Replaces a constant capability expression with a constant test edition; no input is introduced.
        if (ForceOffline) command.CommandText = command.CommandText.Replace("SERVERPROPERTY('EngineEdition')", "2", StringComparison.Ordinal);
#pragma warning restore CA2100
        return ValueTask.FromResult(result);
    }
    public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        var sql = command.CommandText;
        if (sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase) && sql.Contains(WellKnownProperties.ReadModelInstanceInitialized, StringComparison.Ordinal))
        {
            // The migrator's initialized-marker build consumes its own capability check and is not the build under specification.
            _onlineRequested = false;
            return result;
        }
        if (ForceOffline && _onlineRequested && sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase)) UsedOfflineBuild = !sql.Contains("ONLINE = ON", StringComparison.OrdinalIgnoreCase);
        if (sql.Contains("CREATE INDEX", StringComparison.OrdinalIgnoreCase) &&
            ((ForceOffline && _onlineRequested) || sql.Contains("CONCURRENTLY", StringComparison.OrdinalIgnoreCase) || sql.Contains("ONLINE = ON", StringComparison.OrdinalIgnoreCase)) &&
            Interlocked.Exchange(ref _paused, 1) == 0)
        {
            Started.TrySetResult();
            await Continue.Task.WaitAsync(cancellationToken);
        }
        return result;
    }
}
