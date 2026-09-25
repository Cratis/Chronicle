// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Holds the shared rule keeping replay operations off observers the kernel owns.
/// </summary>
/// <remarks>
/// Every replay entry point needs the same answer, and a rule that exists twice is a rule that will eventually be
/// updated once.
/// </remarks>
internal static class KernelOwnedObserverRules
{
    /// <summary>
    /// The message reported when a replay is refused because the kernel owns the observer.
    /// </summary>
    public const string OwnedByKernelMessage = "Observers owned by the kernel cannot be replayed.";

    /// <summary>
    /// Check whether an observer may be replayed.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to read the observer's definition from.</param>
    /// <param name="eventStore">The event store the observer belongs to.</param>
    /// <param name="observerId">The identifier of the observer.</param>
    /// <returns>True when the observer may be replayed, false when the kernel owns it.</returns>
    /// <remarks>
    /// Ownership comes from the stored definition rather than the "$system." identifier prefix, so the guard
    /// cannot be sidestepped by naming an observer carefully. An observer that was never registered passes -
    /// the replay reports that itself, and conflating the two would hide which one happened.
    /// </remarks>
    public static async Task<bool> MayBeReplayed(IStorage storage, string eventStore, string observerId)
    {
        if (string.IsNullOrEmpty(eventStore) || string.IsNullOrEmpty(observerId))
        {
            return true;
        }

        var observers = storage.GetEventStore((EventStoreName)eventStore).Observers;
        var identifier = (ObserverId)observerId;

        if (!await observers.Has(identifier))
        {
            return true;
        }

        var definition = await observers.Get(identifier);
        return definition.Owner != ObserverOwner.Kernel;
    }
}
