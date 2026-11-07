// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Spaces out the restarts of a replay job that the observer's watchdog finds missing.
/// </summary>
/// <remarks>
/// A replay that fails leaves the observer replaying with no live job, which the watchdog answers by starting a new
/// one. Without a delay a replay that can never succeed was restarted on every tick, leaving thousands of failed jobs
/// behind (#5667). The first restart is immediate, so a replay lost to a restart is picked up straight away; each
/// further restart doubles the delay up to <see cref="MaxDelay"/>. A replay job that is seen running resets it.
/// </remarks>
internal sealed class ReplayRestartBackoff
{
    /// <summary>
    /// The delay after the second restart.
    /// </summary>
    internal static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The longest delay between two restarts.
    /// </summary>
    internal static readonly TimeSpan MaxDelay = TimeSpan.FromHours(1);

    DateTimeOffset _notBefore = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets the number of restarts recorded since the last reset.
    /// </summary>
    internal int Attempts { get; private set; }

    /// <summary>
    /// Gets the delay that now applies before the next restart.
    /// </summary>
    internal TimeSpan NextDelay { get; private set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets whether a restart is allowed at the given time.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>True if the next restart may start.</returns>
    internal bool IsDue(DateTimeOffset now) => now >= _notBefore;

    /// <summary>
    /// Records a restart and holds the next one back.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>The number of restarts recorded, including this one.</returns>
    internal int Record(DateTimeOffset now)
    {
        Attempts++;
        NextDelay = Attempts == 1
            ? TimeSpan.Zero
            : TimeSpan.FromTicks(Math.Min(MaxDelay.Ticks, InitialDelay.Ticks << Math.Min(Attempts - 2, 20)));
        _notBefore = now + NextDelay;
        return Attempts;
    }

    /// <summary>
    /// Forgets every recorded restart.
    /// </summary>
    internal void Reset()
    {
        Attempts = 0;
        NextDelay = TimeSpan.Zero;
        _notBefore = DateTimeOffset.MinValue;
    }
}
