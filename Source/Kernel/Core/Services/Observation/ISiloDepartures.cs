// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Tells when a silo starts leaving or leaves the cluster, taking the subscriptions of the observers it hosted with it.
/// </summary>
/// <remarks>
/// It also takes the appended-events queues it hosted, and with them the live subscriptions of observers on the silos
/// that remain, which is why observers re-verify their own subscription on each departure.
/// </remarks>
public interface ISiloDepartures
{
    /// <summary>
    /// Gets a task that completes when the next silo starts leaving or leaves the cluster.
    /// </summary>
    /// <remarks>
    /// Each departure completes the current task and replaces it, so a caller holds on to the task it got until it has
    /// acted on it; reading this again before then can miss a departure.
    /// </remarks>
    Task Next { get; }
}
