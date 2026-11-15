// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.for_EncryptionKeyStorageContract.given;

public sealed class FenceInterleaving : DbCommandInterceptor
{
    TaskCompletionSource _paused;
    TaskCompletionSource _resume;
    bool _publication;
    bool _erasure;
    bool _loseResponse;

    public Task DuringPublication(Func<Task> write, Func<Task> competingOperation)
    {
        _publication = true;
        return During(write, competingOperation);
    }

    public Task DuringErasure(Func<Task> write, Func<Task> competingOperation)
    {
        _erasure = true;
        return During(write, competingOperation);
    }

    public async Task LoseResponse(Func<Task> write)
    {
        _loseResponse = true;
        await write();
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (_erasure && IsFenceUpdate(command))
        {
            _erasure = false;
            await Pause();
        }

        return result;
    }

    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_publication && command.CommandText.StartsWith("INSERT INTO", StringComparison.Ordinal) && command.CommandText.Contains("EncryptionKeys", StringComparison.Ordinal))
        {
            _publication = false;
            await Pause();
        }

        if (_loseResponse && result > 0 && IsFenceUpdate(command))
        {
            _loseResponse = false;
            throw new TimeoutException("Applied fence write response was lost");
        }

        return result;
    }

    static bool IsFenceUpdate(DbCommand command) => command.CommandText.StartsWith("UPDATE", StringComparison.Ordinal) && command.CommandText.Contains("EncryptionKeyErasures", StringComparison.Ordinal);

    async Task Pause()
    {
        _paused.TrySetResult();
        await _resume.Task;
    }

    async Task During(Func<Task> operation, Func<Task> competingOperation)
    {
        _paused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _resume = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = operation();
        try
        {
            await _paused.Task.WaitAsync(TimeSpan.FromSeconds(30));
            await competingOperation();
        }
        finally
        {
            _resume.TrySetResult();
        }

        await pending;
    }
}
