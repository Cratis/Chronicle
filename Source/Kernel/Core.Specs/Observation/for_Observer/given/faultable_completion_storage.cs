// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class faultable_completion_storage<T>(Func<T, T> snapshot, Func<T, T>? rehydrate = null) : IStorage<T>, IStorageStats
    where T : new()
{
    public T State { get; set; } = new();
    public T PersistedState { get; private set; } = new();
    public TestStorageStats Stats { get; } = new();
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }
    public int? FailOnWrite { get; set; }
    public bool LoseAcknowledgement { get; set; }

    public Task ReadStateAsync()
    {
        State = (rehydrate ?? snapshot)(PersistedState);
        Stats.Reads++;
        return Task.CompletedTask;
    }

    public Task WriteStateAsync()
    {
        Stats.Writes++;
        var fail = FailOnWrite == Stats.Writes;
        if (!fail || LoseAcknowledgement)
        {
            PersistedState = snapshot(State);
            RecordExists = true;
        }

        if (fail)
        {
            FailOnWrite = null;
            throw new InvalidOperationException("Injected completion write failure");
        }

        return Task.CompletedTask;
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
