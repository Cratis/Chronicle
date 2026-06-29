// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.InMemory.Keys;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverKeyIndexes"/>.
/// </summary>
public sealed class ObserverKeyIndexes : IObserverKeyIndexes
{
    readonly ConcurrentDictionary<ObserverKey, IObserverKeyIndex> _indexes = new();

    /// <inheritdoc/>
    public Task<IObserverKeyIndex> GetFor(ObserverKey observerKey) =>
        Task.FromResult(_indexes.GetOrAdd(observerKey, _ => new ObserverKeyIndex()));
}
