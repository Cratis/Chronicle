// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.given;

/// <summary>
/// Stores independent snapshots of job step checkpoints for restart specifications.
/// </summary>
public class SnapshottingHandleEventsForObserverStorage : IPersistentState<HandleEventsForObserverState>
{
    /// <inheritdoc/>
    public HandleEventsForObserverState State { get; set; } = new();

    /// <summary>
    /// Gets the last persisted checkpoint, independent of the working state.
    /// </summary>
    public HandleEventsForObserverState PersistedState { get; private set; } = new();

    /// <inheritdoc/>
    public string Etag => string.Empty;

    /// <inheritdoc/>
    public bool RecordExists { get; private set; }

    /// <inheritdoc/>
    public Task WriteStateAsync()
    {
        PersistedState = ReplaySerialization.Restore(State);
        RecordExists = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReadStateAsync()
    {
        State = ReplaySerialization.Restore(PersistedState);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ClearStateAsync()
    {
        State = new();
        PersistedState = new();
        RecordExists = false;
        return Task.CompletedTask;
    }
}
