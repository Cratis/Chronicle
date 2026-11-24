// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache.given;

public sealed class ControlledProvenance : ISinkProvenanceStorage
{
    readonly InMemorySinkProvenanceStorage _storage = new();
    TaskCompletionSource<SinkProvenanceState>? _held;
    SinkProvenanceState? _captured;

    public bool Hold { get; set; }

    public int Reads { get; set; }

    public bool HasHeldRead => _held is not null;

    public async Task<SinkProvenanceState> ReadState()
    {
        Reads++;
        var state = await _storage.ReadState();
        if (!Hold)
        {
            return state;
        }

        _captured = state;
        _held = new();
        return await _held.Task.ConfigureAwait(false);
    }

    public void Release()
    {
        Hold = false;
        _held!.SetResult(_captured!);
        _held = null;
    }

    public Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence) => _storage.CompleteInventory(restrictedEvidence);

    public Task<bool> IsQuarantined() => _storage.IsQuarantined();

    public Task MarkQuarantined() => _storage.MarkQuarantined();
}
