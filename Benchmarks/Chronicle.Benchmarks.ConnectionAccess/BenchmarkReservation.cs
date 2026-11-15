// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Benchmarks.ConnectionAccess;

/// <summary>
/// Exercises the real lease and liveness checks without an Orleans activation runtime.
/// </summary>
/// <param name="membership">The active cluster membership.</param>
internal sealed class BenchmarkReservation(IClusterMembershipService membership) : ConnectionReservation(membership)
{
    /// <inheritdoc/>
    internal override void Keep(TimeSpan period)
    {
    }

    /// <inheritdoc/>
    internal override void DeactivateWhenIdle()
    {
    }
}
