// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Tracks changes to settled observer running states without stamping transient routing states.
/// </summary>
internal static class ObserverRunningStateChanges
{
    /// <summary>
    /// Applies a running state, reporting a change only when the settled state differs.
    /// </summary>
    /// <param name="changedAt">The time the running state last changed, or null when unknown.</param>
    /// <param name="settled">The last settled running state, seeded from storage on activation.</param>
    /// <param name="next">The running state being entered.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The time of the last change, the last settled running state and whether the settled state changed.</returns>
    public static (DateTimeOffset? ChangedAt, ObserverRunningState Settled, bool Changed) Apply(
        DateTimeOffset? changedAt,
        ObserverRunningState settled,
        ObserverRunningState next,
        DateTimeOffset now)
    {
        var changed = next != ObserverRunningState.Unknown && next != settled;
        return changed ? (now, next, true) : (changedAt, settled, false);
    }
}
