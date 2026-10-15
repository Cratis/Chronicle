// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Controls durable occurrence delivery independently of observer retries.
/// </summary>
public class Occurrences
{
    /// <summary>
    /// Gets the maximum simultaneous live leases per trigger.
    /// </summary>
    public int MaxInFlight { get; init; } = 1;

    /// <summary>
    /// Gets the attempt limit before quarantine; zero permits unlimited attempts.
    /// </summary>
    public int MaxAttempts { get; init; } = 10;

    /// <summary>
    /// Gets the initial retry delay in seconds.
    /// </summary>
    public int BackoffDelay { get; init; } = 1;

    /// <summary>
    /// Gets the exponential retry factor.
    /// </summary>
    public float ExponentialBackoffDelayFactor { get; init; } = 2;

    /// <summary>
    /// Gets the maximum retry delay in seconds.
    /// </summary>
    public int MaximumBackoffDelay { get; init; } = 600;
}
