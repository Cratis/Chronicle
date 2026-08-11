// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Computes how long a registration run should wait before hitting the wire again after failed runs.
/// </summary>
/// <remarks>
/// The delay grows exponentially with the number of consecutive failures and is capped, so a kernel that is slow to
/// accept a large registration sees the pressure back off instead of build up. The delay is jittered to between half
/// and the full exponential value, so replicas that failed together do not retry together.
/// </remarks>
/// <param name="initialDelay">Delay after the first failure. Defaults to one second.</param>
/// <param name="maximumDelay">Upper bound for the delay. Defaults to one minute.</param>
/// <param name="jitterSource">Source of jitter values in [0, 1). Defaults to <see cref="Random.Shared"/>.</param>
internal sealed class RegistrationBackoff(TimeSpan? initialDelay = null, TimeSpan? maximumDelay = null, Func<double>? jitterSource = null)
{
    readonly TimeSpan _initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
    readonly TimeSpan _maximumDelay = maximumDelay ?? TimeSpan.FromMinutes(1);
    readonly Func<double> _jitterSource = jitterSource ?? Random.Shared.NextDouble;

    /// <summary>
    /// Get the delay to wait before the next run.
    /// </summary>
    /// <param name="consecutiveFailures">Number of runs that have failed in a row. Zero means the previous run succeeded or there has been none.</param>
    /// <returns>The <see cref="TimeSpan"/> to wait; <see cref="TimeSpan.Zero"/> when there is nothing to back off from.</returns>
    public TimeSpan NextDelay(int consecutiveFailures)
    {
        if (consecutiveFailures <= 0)
        {
            return TimeSpan.Zero;
        }

        var exponential = _initialDelay * Math.Pow(2, consecutiveFailures - 1);
        if (exponential > _maximumDelay)
        {
            exponential = _maximumDelay;
        }

        return exponential * (0.5 + (0.5 * _jitterSource()));
    }
}
