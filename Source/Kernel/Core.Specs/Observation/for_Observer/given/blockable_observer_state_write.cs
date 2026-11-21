// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

/// <summary>
/// Observer state storage whose writes can be held in flight until the spec lets them finish.
/// </summary>
public class blockable_observer_state_write : IStorage<ObserverState>, IStorageStats
{
    public TaskCompletionSource Writing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ObserverState State { get; set; } = new();
    public TestStorageStats Stats { get; } = new() { Reads = -1 };
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public bool Block { get; set; }
    public int Completed { get; private set; }

    public Task ReadStateAsync()
    {
        Stats.Reads++;
        return Task.CompletedTask;
    }

    public async Task WriteStateAsync()
    {
        if (Block)
        {
            Writing.TrySetResult();
            await Release.Task;
        }

        RecordExists = true;
        Stats.Writes++;
        Completed++;
    }

    public Task ClearStateAsync()
    {
        State = new();
        RecordExists = false;
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
