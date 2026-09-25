// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the outcome of asking an event store to remove an observer.
/// </summary>
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
