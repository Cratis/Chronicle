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
    /// <remarks>
    /// The in-memory index holds the keys that were added to it and has no events to read, so it enumerates every
    /// key it holds whichever scope is asked for.
    /// </remarks>
    public Task<IObserverKeyIndex> GetFor(ObserverKey observerKey, ObserverReaderScope scope) =>
        Task.FromResult(_indexes.GetOrAdd(observerKey, _ => new ObserverKeyIndex()));
}
