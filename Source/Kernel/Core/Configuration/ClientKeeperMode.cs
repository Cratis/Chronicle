// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Defines how the open observation stream of a client keeps the subscriptions of its reactors and reducers in place (decision 0034).
/// </summary>
public enum ClientKeeperMode
{
    /// <summary>
    /// Ask the client's observer grains on every check, which keeps them and the observers they serve resident.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Treat a subscription as intact while the stream is open and the observer's durable subscription names the client,
    /// without calling any grain, so a dormant observer stays dormant. Falls back to <see cref="Legacy"/> for an observer
    /// that has no durable subscription, and whenever dormant release does not apply (it is off, or the cluster is mixed).
    /// </summary>
    SiloLocal = 1
}
