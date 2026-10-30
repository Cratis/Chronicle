// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Computes how long a client can go without a handled response before it may have been dropped as stale.
/// </summary>
internal static class KeepAliveLapse
{
    /// <summary>
    /// Gets the longest gap between responses from a client that cannot have got it dropped as stale.
    /// </summary>
    /// <remarks>
    /// The registry drops a client whose last response is older than the stale threshold when it checks. A response
    /// reaches the registry a moment after it reaches the service, so a gap within one keep-alive of the threshold is
    /// already treated as one that may have got the client dropped.
    /// </remarks>
    /// <param name="options">The <see cref="Configuration.Clients"/> configuration.</param>
    /// <returns>The lapse as a <see cref="TimeSpan"/>.</returns>
    public static TimeSpan From(Configuration.Clients options) => TimeSpan.FromSeconds(Math.Max(
        options.StaleThresholdSeconds - options.KeepAliveIntervalSeconds,
        options.KeepAliveIntervalSeconds));
}
