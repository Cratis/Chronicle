// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Core;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op, in-memory implementation of <see cref="IStorage{TState}"/> that allows
/// kernel grains to be instantiated without Orleans infrastructure.
/// </summary>
/// <typeparam name="TState">The state type the grain persists.</typeparam>
internal sealed class InMemoryGrainStorage<TState> : IStorage<TState>
    where TState : new()
{
    /// <inheritdoc/>
    public TState State { get; set; } = new();

    /// <inheritdoc/>
    public string Etag { get; } = string.Empty;

    /// <inheritdoc/>
    public bool RecordExists { get; set; }

    /// <inheritdoc/>
    public Task ReadStateAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task WriteStateAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ClearStateAsync() => Task.CompletedTask;
}
