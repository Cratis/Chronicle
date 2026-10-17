// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class blockable_definition_write : IStorage<ObserverDefinition>, IStorageStats
{
    public TaskCompletionSource Writing { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ObserverDefinition State { get; set; } = new();
    public TestStorageStats Stats { get; } = new();
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public bool Block { get; set; }

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
    }

    public Task ClearStateAsync()
    {
        RecordExists = false;
        State = new();
        Stats.Clears++;
        return Task.CompletedTask;
    }
}
