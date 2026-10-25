// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;
using Orleans.Storage;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class provider_backed_storage<T>(IGrainStorage provider, GrainId grainId) : IStorage<T>, IStorageStats
{
    readonly GrainState<T> _state = new();

    public T State { get => _state.State!; set => _state.State = value; }
    public TestStorageStats Stats { get; } = new();
    public string Etag => string.Empty;
    public bool RecordExists { get; private set; }

    public async Task ReadStateAsync()
    {
        await provider.ReadStateAsync(typeof(T).Name, grainId, _state);
        Stats.Reads++;
    }

    public async Task WriteStateAsync()
    {
        await provider.WriteStateAsync(typeof(T).Name, grainId, _state);
        RecordExists = true;
        Stats.Writes++;
    }

    public async Task ClearStateAsync()
    {
        await provider.ClearStateAsync(typeof(T).Name, grainId, _state);
        RecordExists = false;
        Stats.Clears++;
    }
}
