// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Represents the outcome of asking an event store to remove an observer.
/// </summary>
/// <remarks>
/// Removal exists for the observer whose declaring code is gone: delete a read model and its projection, or remove a
/// reactor, and the observer it registered stays behind forever, settling into <see cref="ObserverRunningState.Disconnected"/>
/// with records in the store-level and namespaced observer collections, its projection definition, its handled counts
/// and its failed partitions. An observer that any namespace still reports as running or that still has a subscribed
/// client is a live observer, not an abandoned one, so removal refuses rather than tearing it out from under a
/// running application.
/// </remarks>
public enum ObserverRemovalOutcome
{
    /// <summary>
    /// The observer and everything keyed to it was removed.
    /// </summary>
    Removed = 0,

    /// <summary>
    /// No observer with that identifier is registered in the event store, so there was nothing to remove.
    /// </summary>
    ObserverNotFound = 1,

    /// <summary>
    /// The observer is running in at least one namespace, so it is still a live observer and cannot be removed.
    /// </summary>
    ObserverActive = 2,

    /// <summary>
    /// A client is still subscribed to the observer in at least one namespace, so its declaring code is still present.
    /// </summary>
    ObserverSubscribed = 3
}
