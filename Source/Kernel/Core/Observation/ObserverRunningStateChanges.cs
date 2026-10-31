// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Tracks changes to settled observer running states without stamping transient routing states.
/// </summary>
internal static class ObserverRunningStateChanges
{
    /// <summary>
    /// Applies a running state, recording the time only when the settled state changes.
    /// </summary>
    /// <param name="state">The current observer state.</param>
    /// <param name="settled">The last settled running state, seeded from storage on activation.</param>
    /// <param name="next">The running state being entered.</param>
    /// <param name="now">The current UTC time.</param>
    /// <returns>The updated observer state and last settled running state.</returns>
    public static (ObserverState State, ObserverRunningState Settled) Apply(ObserverState state, ObserverRunningState settled, ObserverRunningState next, DateTimeOffset now)
    {
        var changed = next != ObserverRunningState.Unknown && next != settled;
        return (state with { RunningState = next, RunningStateChangedAt = changed ? now : state.RunningStateChangedAt }, changed ? next : settled);
    }
}
