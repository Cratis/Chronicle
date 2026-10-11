// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using Cratis.Chronicle.Storage.EventSequences;
using Orleans.Core;
using KernelSequences = KernelCore::Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Adapts the production event-sequence state provider to a directly constructed grain.
/// </summary>
/// <param name="storage">The scenario's shared storage.</param>
/// <param name="key">The event sequence identity.</param>
internal sealed class EventSequenceGrainStorage(
    Storage.IStorage storage,
    KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.EventSequenceKey key) : IStorage<EventSequenceState>
{
    readonly KernelSequences::EventSequencesStorageProvider _provider = new(storage);
    readonly GrainState<EventSequenceState> _state = new(new EventSequenceState());
    readonly GrainId _grainId = GrainId.Create("eventsequence", key.ToString());

    /// <inheritdoc/>
    public EventSequenceState State { get => _state.State!; set => _state.State = value; }

    /// <inheritdoc/>
    public string Etag => _state.ETag ?? string.Empty;

    /// <inheritdoc/>
    public bool RecordExists { get => _state.RecordExists; set => _state.RecordExists = value; }

    /// <inheritdoc/>
    public Task ReadStateAsync() => _provider.ReadStateAsync(nameof(EventSequenceState), _grainId, _state);

    /// <inheritdoc/>
    public Task WriteStateAsync() => _provider.WriteStateAsync(nameof(EventSequenceState), _grainId, _state);

    /// <inheritdoc/>
    public Task ClearStateAsync() => _provider.ClearStateAsync(nameof(EventSequenceState), _grainId, _state);
}
