// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_OccurrenceProcessRecovery;

public sealed class PostCommitBarrier(bool hold) : IDisposable
{
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly SemaphoreSlim _output = new(1, 1);
    int _entered;

    public bool IsHeld => hold && Volatile.Read(ref _entered) != 0 && !_release.Task.IsCompleted;

    public async Task AfterCommit(string settlement)
    {
        Interlocked.Exchange(ref _entered, 1);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(240));
        await Report(settlement, deadline.Token);

        // Deliberately outlive a lost connection/lease: only the parent's release (or deadline)
        // may let this handler return. Kernel death must not silently release the barrier.
        if (hold) await _release.Task.WaitAsync(deadline.Token);
    }

    public void Release() => _release.TrySetResult();

    public void Dispose() => _output.Dispose();

    public async Task Report(string line, CancellationToken cancellationToken)
    {
        await _output.WaitAsync(cancellationToken);
        try
        {
            await Console.Out.WriteLineAsync(line.AsMemory(), cancellationToken);
            await Console.Out.FlushAsync(cancellationToken);
        }
        finally
        {
            _output.Release();
        }
    }
}
