// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

/// <summary>
/// Represents monotonic provenance for one in-memory storage scope.
/// </summary>
public class InMemorySinkProvenanceStorage : ISinkProvenanceStorage
{
    readonly Lock _gate = new();
    SinkProvenanceState _state = new(new(Guid.NewGuid()), 0, false);

    /// <inheritdoc/>
    public Task<SinkProvenanceState> ReadState()
    {
        lock (_gate) return Task.FromResult(_state);
    }

    /// <inheritdoc/>
    public Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence)
    {
        lock (_gate)
        {
            if (!_state.IsComplete)
            {
                _state = _state with { InventoryVersion = SinkProvenanceState.CurrentInventoryVersion, Restricted = _state.Restricted || restrictedEvidence };
            }

            return Task.FromResult(_state);
        }
    }

    /// <inheritdoc/>
    public Task<bool> IsQuarantined()
    {
        lock (_gate) return Task.FromResult(_state.Restricted);
    }

    /// <inheritdoc/>
    public Task MarkQuarantined()
    {
        lock (_gate) _state = _state with { Restricted = true };
        return Task.CompletedTask;
    }
}
