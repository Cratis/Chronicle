// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Limits how many suspected-skip warnings are written in a time window. Counters are never limited, only the log.
/// </summary>
/// <param name="maximumPerWindow">The number of warnings admitted per window.</param>
/// <param name="windowMilliseconds">The length of a window in milliseconds.</param>
/// <param name="clock">Optional clock returning milliseconds, defaults to <see cref="Environment.TickCount64"/>.</param>
/// <remarks>
/// Thread safe, because the appended-events queue dispatches observers concurrently.
/// </remarks>
internal sealed class SuspectedSkipWarningLimiter(int maximumPerWindow = 10, long windowMilliseconds = 60_000, Func<long>? clock = null)
{
    readonly Lock _lock = new();
    readonly Func<long> _clock = clock ?? (() => Environment.TickCount64);
    bool _started;
    long _windowStart;
    int _admitted;
    int _suppressed;

    /// <summary>
    /// Gets the window length in whole seconds.
    /// </summary>
    internal int WindowSeconds => (int)(windowMilliseconds / 1000);

    /// <summary>
    /// Tries to admit a warning.
    /// </summary>
    /// <param name="suppressedInPreviousWindow">The number of warnings that were not admitted in the window that just ended, reported once when a new window opens.</param>
    /// <returns>True if the warning may be written.</returns>
    internal bool TryAdmit(out int suppressedInPreviousWindow)
    {
        lock (_lock)
        {
            suppressedInPreviousWindow = 0;
            var now = _clock();
            if (!_started || now - _windowStart >= windowMilliseconds)
            {
                suppressedInPreviousWindow = _suppressed;
                _suppressed = 0;
                _admitted = 0;
                _windowStart = now;
                _started = true;
            }

            if (_admitted >= maximumPerWindow)
            {
                _suppressed++;
                return false;
            }

            _admitted++;
            return true;
        }
    }
}
