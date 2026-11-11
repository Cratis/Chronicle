// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Tells when a silo leaves the cluster, taking the subscriptions of the observers it hosted with it.
/// </summary>
public interface ISiloDepartures
{
    /// <summary>
    /// Gets a task that completes when the next silo leaves the cluster.
    /// </summary>
    Task Next { get; }
}
