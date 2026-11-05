// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Kernel infrastructure: defines the <see cref="ProjectionWatchGate"/> of every projection on this silo.
/// </summary>
public interface IProjectionWatchGates
{
    /// <summary>
    /// Gets the gate of a projection; the same instance every time for the same key.
    /// </summary>
    /// <param name="key">The <see cref="ProjectionChangesetNotifierKey"/> identifying the projection in an event store namespace.</param>
    /// <returns>The <see cref="ProjectionWatchGate"/>.</returns>
    ProjectionWatchGate GateFor(ProjectionChangesetNotifierKey key);

    /// <summary>
    /// Opens the gate of a projection for the standard period.
    /// </summary>
    /// <param name="key">The <see cref="ProjectionChangesetNotifierKey"/> identifying the projection in an event store namespace.</param>
    void Open(ProjectionChangesetNotifierKey key);

    /// <summary>
    /// Closes the gate of a projection.
    /// </summary>
    /// <param name="key">The <see cref="ProjectionChangesetNotifierKey"/> identifying the projection in an event store namespace.</param>
    void Close(ProjectionChangesetNotifierKey key);
}
