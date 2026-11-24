// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_CatchUpAdmission.given;

/// <summary>
/// A <see cref="TimeProvider"/> that only moves when told to.
/// </summary>
public class ManualTimeProvider : TimeProvider
{
    DateTimeOffset _now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => _now;

    /// <summary>
    /// Moves the time forward.
    /// </summary>
    /// <param name="by">How far to move it.</param>
    public void Advance(TimeSpan by) => _now += by;
}
