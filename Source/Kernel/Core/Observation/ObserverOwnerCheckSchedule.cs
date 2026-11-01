// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Spaces out the keep-alive checks of a client registration for a newer owner on another event sequence.
/// </summary>
/// <remarks>
/// The first check is due straight away, so the first keep-alive after activation checks. Each started check
/// makes the next one due after the interval. An interval of zero or less makes every check due.
/// </remarks>
/// <param name="clock">The <see cref="TimeProvider"/> to read the time from.</param>
/// <param name="interval">The time between checks.</param>
internal sealed class ObserverOwnerCheckSchedule(TimeProvider clock, TimeSpan interval)
{
    DateTimeOffset? _nextCheck;

    /// <summary>
    /// Starts a check when one is due.
    /// </summary>
    /// <returns>True when a check is due and has been started; false when the previous check is too recent.</returns>
    public bool TryStart()
    {
        var now = clock.GetUtcNow();
        if (now < _nextCheck)
        {
            return false;
        }

        _nextCheck = now + interval;
        return true;
    }
}
