// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class controlled_observer_state_storage : IStorage<ObserverState>, IStorageStats
{
    public ObserverState State { get; set; } = new();
    public ObserverState PersistedState { get; private set; } = new();
    public TestStorageStats Stats { get; } = new() { Reads = -1 };
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public int Attempts { get; private set; }
    public Func<Task> BeforeWrite { get; set; } = () => Task.CompletedTask;

    public Task ReadStateAsync()
    {
        Stats.Reads++;
        State = PersistedState;
        return Task.CompletedTask;
    }

    public async Task WriteStateAsync()
    {
        Attempts++;
        var snapshot = State;
        await BeforeWrite();
        PersistedState = snapshot;
        RecordExists = true;
        Stats.Writes++;
    }

    public Task ClearStateAsync()
    {
        State = new();
        PersistedState = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
