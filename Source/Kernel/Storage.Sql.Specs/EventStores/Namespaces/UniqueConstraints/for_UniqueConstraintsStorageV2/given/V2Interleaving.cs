// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorageV2.given;

public class V2Interleaving : DbTransactionInterceptor
{
    TaskCompletionSource _paused = NewSignal();
    TaskCompletionSource _resume = NewSignal();
    TaskCompletionSource _transitionAttempted = NewSignal();
    int _starts;
    int _pauseOnce;
    bool _enabled;
    bool _transitionFirst;

    public V2FenceCommands Commands => new(this);

    /// <summary>
    /// Gets whether the competing transition has reached its provider-specific waiting boundary.
    /// </summary>
    internal bool TransitionWasAttempted => _transitionAttempted.Task.IsCompleted;

    public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
    {
        if (!_enabled) return result;
        var starts = Interlocked.Increment(ref _starts);
        if (starts == 1 && _transitionFirst) await Pause();
        if (starts == 2) _transitionAttempted.TrySetResult();
        return result;
    }

    public Task FencePassed() => _enabled && !_transitionFirst && Interlocked.Exchange(ref _pauseOnce, 1) == 0 ? Pause() : Task.CompletedTask;

    public void TransitionReading()
    {
        // A SQL Server transition can block on the fenced row during its precheck, before transaction start.
        if (_enabled && !_transitionFirst && Volatile.Read(ref _pauseOnce) == 1) _transitionAttempted.TrySetResult();
    }

    public async Task<Result<bool, UniqueConstraintWriteFailure>> Run(Func<Task> transition, Func<Task<Result<bool, UniqueConstraintWriteFailure>>> mutation, bool transitionFirst)
    {
        _transitionFirst = transitionFirst;
        _enabled = true;
        var writer = Task.Run(mutation);
        var switching = Task.CompletedTask;
        try
        {
            await _paused.Task.WaitAsync(TimeSpan.FromSeconds(30));
            switching = Task.Run(transition);
            if (transitionFirst) await switching.WaitAsync(TimeSpan.FromSeconds(30));
            else await _transitionAttempted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            _resume.TrySetResult();
            await Task.WhenAll(writer, switching).WaitAsync(TimeSpan.FromSeconds(40));
            return await writer;
        }
        finally
        {
            _resume.TrySetResult();
            await Task.WhenAll(writer, switching).WaitAsync(TimeSpan.FromSeconds(40));
            _enabled = false;
        }
    }

    static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Pause()
    {
        _paused.TrySetResult();
        await _resume.Task.WaitAsync(TimeSpan.FromSeconds(40));
    }
}
