// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Namespaces;
using Cratis.Chronicle.Discovery;

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
    /// Determines whether a registration failure can be retried without operator intervention.
    /// </summary>
    /// <param name="exception">The failure, potentially wrapping a structured kernel command result.</param>
    /// <returns>False for local preparation failures and the literal namespace recovery-required code.</returns>
    public static bool IsRetryable(Exception exception) =>
        exception is not (ArtifactIdentityDriftDetected or ArtifactPreparationReentered) && !RequiresNamespaceRecovery(exception);

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

        // Clamp in floating-point ticks before constructing a TimeSpan: multiplying a TimeSpan by 2^n overflows
        // once n passes ~43, and a run that keeps failing reaches that within the hour.
        var exponentialTicks = Math.Min(_initialDelay.Ticks * Math.Pow(2, consecutiveFailures - 1), _maximumDelay.Ticks);
        var jitteredTicks = exponentialTicks * (0.5 + (0.5 * _jitterSource()));

        return TimeSpan.FromTicks((long)jitteredTicks);
    }

    static bool RequiresNamespaceRecovery(Exception exception) => exception switch
    {
        CommandFailed { Result: { } result } when result.ExceptionMessages.Contains(NamespaceErrorCodes.RecoveryRequired, StringComparer.Ordinal) => true,
        AggregateException aggregate => aggregate.InnerExceptions.Any(RequiresNamespaceRecovery),
        { InnerException: { } inner } => RequiresNamespaceRecovery(inner),
        _ => false
    };
}
