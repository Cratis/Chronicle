// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Orleans.Core;
using Orleans.TestKit.Storage;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class provider_backed_observer_state_storage(ObserverStateGrainStorageProvider provider, ObserverKey observerKey) : IStorage<ObserverState>, IStorageStats
{
    readonly GrainState<ObserverState> _grainState = new(new ObserverState());
    readonly GrainId _grainId = GrainId.Create(nameof(Observer), observerKey.ToString());

    public ObserverState State
    {
        get => _grainState.State!;
        set => _grainState.State = value;
    }

    public TestStorageStats Stats { get; } = new();
    public string Etag => _grainState.ETag ?? string.Empty;
    public bool RecordExists => _grainState.RecordExists;

    public async Task ReadStateAsync()
    {
        await provider.ReadStateAsync(nameof(ObserverState), _grainId, _grainState);
        Stats.Reads++;
    }

    public async Task WriteStateAsync()
    {
        await provider.WriteStateAsync(nameof(ObserverState), _grainId, _grainState);
        Stats.Writes++;
    }

    public Task ClearStateAsync() => provider.ClearStateAsync(nameof(ObserverState), _grainId, _grainState);
}
