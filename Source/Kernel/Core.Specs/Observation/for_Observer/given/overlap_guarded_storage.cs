// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Mirrors the Orleans state bridge: a read that starts while another read of the same state is in flight throws.
/// A read can be held open so a spec decides, without timing, what overlaps it.
/// </summary>
/// <typeparam name="T">The type of state.</typeparam>
/// <param name="inner">The storage that does the reading.</param>
public class overlap_guarded_storage<T>(IStorage<T> inner) : IStorage<T>, IStorageStats
{
    int _inFlight;

    public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool HoldNextRead { get; set; }
    public int MaxReadsInFlight { get; private set; }
    public T State { get => inner.State!; set => inner.State = value; }
    public TestStorageStats Stats => ((IStorageStats)inner).Stats!;
    public string Etag => inner.Etag ?? string.Empty;
    public bool RecordExists => inner.RecordExists;

    public async Task ReadStateAsync()
    {
        if (Interlocked.Increment(ref _inFlight) > 1)
        {
            Interlocked.Decrement(ref _inFlight);
            throw new InvalidOperationException("Reading is already in progress.");
        }

        try
        {
            MaxReadsInFlight = Math.Max(MaxReadsInFlight, _inFlight);
            if (HoldNextRead)
            {
                HoldNextRead = false;
                ReadStarted.TrySetResult();
                await ReleaseRead.Task;
            }

            await inner.ReadStateAsync();
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public Task WriteStateAsync() => inner.WriteStateAsync();

    public Task ClearStateAsync() => inner.ClearStateAsync();
}
