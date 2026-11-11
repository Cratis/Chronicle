// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class interceptable_observer_state_storage : IStorage<ObserverState>, IStorageStats
{
    public TestStorageStats Stats { get; } = new();
    public ObserverState State { get; set; } = new();
    public string Etag { get; set; } = string.Empty;
    public bool RecordExists { get; set; }
    public List<ObserverState> Writes { get; } = [];
    public Func<Task> OnWrite { get; set; } = () => Task.CompletedTask;

    public Task ClearStateAsync()
    {
        State = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }

    public Task ReadStateAsync()
    {
        Stats.Reads++;
        return Task.CompletedTask;
    }

    public async Task WriteStateAsync()
    {
        Writes.Add(State);
        await OnWrite();
        RecordExists = true;
        Stats.Writes++;
    }
}
