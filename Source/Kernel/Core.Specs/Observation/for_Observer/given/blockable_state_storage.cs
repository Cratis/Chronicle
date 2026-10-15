// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class blockable_state_storage<TState> : IStorage<TState>, IStorageStats
    where TState : new()
{
    public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TState State { get; set; } = new();
    public TestStorageStats Stats { get; } = new() { Reads = -1 };
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public bool SuspendNextRead { get; set; }

    public async Task ReadStateAsync()
    {
        Stats.Reads++;
        if (SuspendNextRead)
        {
            SuspendNextRead = false;
            ReadStarted.SetResult();
            await ReleaseRead.Task;
        }
    }

    public Task WriteStateAsync()
    {
        RecordExists = true;
        Stats.Writes++;
        return Task.CompletedTask;
    }

    public Task ClearStateAsync()
    {
        State = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
