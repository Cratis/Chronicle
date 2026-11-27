// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class reloadable_observer_state_storage : IStorage<ObserverState>, IStorageStats
{
    public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource ReleaseRead { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ObserverState PersistedState { get; set; } = new();
    public ObserverState State { get; set; } = new();
    public TestStorageStats Stats { get; } = new() { Reads = -1 };
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public bool SuspendNextRead { get; set; }
    public Func<ObserverState, Task> BeforeWrite { get; set; } = _ => Task.CompletedTask;
    public Func<ObserverState, Task> AfterWrite { get; set; } = _ => Task.CompletedTask;

    public async Task ReadStateAsync()
    {
        var snapshot = PersistedState;
        Stats.Reads++;
        if (SuspendNextRead)
        {
            SuspendNextRead = false;
            ReadStarted.SetResult();
            await ReleaseRead.Task;
        }
        State = snapshot;
    }

    public async Task WriteStateAsync()
    {
        var snapshot = State;
        await BeforeWrite(snapshot);
        PersistedState = snapshot;
        RecordExists = true;
        Stats.Writes++;
        await AfterWrite(snapshot);
    }

    public Task ClearStateAsync()
    {
        PersistedState = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
