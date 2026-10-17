// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Drives occurrence lease expiry and retry eligibility explicitly. Advancing it never delivers work or fires timers.
/// Event append timestamps still use the kernel's system clock.
/// </summary>
/// <param name="now">The initial instant.</param>
public sealed class ScenarioTime(DateTimeOffset now) : TimeProvider
{
    readonly object _sync = new();
    DateTimeOffset _now = now.ToUniversalTime();

    /// <summary>
    /// Initializes the clock at midnight UTC on January 1, 2026.
    /// </summary>
    public ScenarioTime() : this(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))
    {
    }

    /// <summary>
    /// Gets the current UTC instant.
    /// </summary>
    public DateTimeOffset Now
    {
        get
        {
            lock (_sync) return _now;
        }
    }

    /// <inheritdoc/>
    public override DateTimeOffset GetUtcNow() => Now;

    /// <summary>
    /// Advances by a non-negative duration without delivering work.
    /// </summary>
    /// <param name="duration">The elapsed duration.</param>
    /// <exception cref="ScenarioTimeCannotMoveBackwards">The duration is negative.</exception>
    public void Advance(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero) throw new ScenarioTimeCannotMoveBackwards();
        lock (_sync) AdvanceTo(_now.Add(duration));
    }

    /// <summary>
    /// Advances to an equal or later instant without delivering work.
    /// </summary>
    /// <param name="instant">The target instant.</param>
    /// <exception cref="ScenarioTimeCannotMoveBackwards">The instant precedes the current instant.</exception>
    public void AdvanceTo(DateTimeOffset instant)
    {
        lock (_sync)
        {
            if (instant < _now) throw new ScenarioTimeCannotMoveBackwards();
            _now = instant.ToUniversalTime();
        }
    }
}
