// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.Kernel.Keys;

namespace Cratis.Kernel.Storage.Keys;

/// <summary>
/// Defines a system for indexing keys for an observer.
/// </summary>
public interface IObserverKeyIndex
{
    /// <summary>
    /// Get the keys starting from a <see cref="EventSequenceNumber"/> .
    /// </summary>
    /// <param name="fromEventSequenceNumber">The from <see cref="EventSequenceNumber"/> to get keys from.</param>
    /// <returns>All the <see cref="IObserverKeys"/>.</returns>
    IObserverKeys GetKeys(EventSequenceNumber fromEventSequenceNumber);

    /// <summary>
    /// Add a key to the index.
    /// </summary>
    /// <param name="key"><see cref="Key"/> to add.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(Key key);

    /// <summary>
    /// Rebuild the index.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Rebuild();
}
