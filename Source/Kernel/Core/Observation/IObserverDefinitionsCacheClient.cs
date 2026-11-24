// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a client that tells the observer definition registry of every silo in the cluster that a definition changed.
/// </summary>
public interface IObserverDefinitionsCacheClient
{
    /// <summary>
    /// Announce that the definition of an observer was saved or deleted, once the storage holds the change.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> whose definition changed.</param>
    /// <returns>Awaitable task.</returns>
    Task Invalidate(EventStoreName eventStore, ObserverId observerId);
}
