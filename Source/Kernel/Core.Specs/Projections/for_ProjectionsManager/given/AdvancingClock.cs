// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.given;

/// <summary>
/// A <see cref="TimeProvider"/> whose time only moves when a specification advances it.
/// </summary>
public class AdvancingClock : TimeProvider
{
    DateTimeOffset _now = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>
    /// Moves time forward.
    /// </summary>
    /// <param name="by">How far to move it.</param>
    public void Advance(TimeSpan by) => _now += by;
}
