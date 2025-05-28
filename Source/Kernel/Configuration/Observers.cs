// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the configuration for observers.
/// </summary>
public class Observers
{
    /// <summary>
    /// Gets the timeout in seconds for observer calling its subscriber.
    /// </summary>
    public int SubscriberTimeout { get; init; } = 5;

    /// <summary>
    /// Gets the maximum number of retries that can be attempted on a failed observer partition.
    /// </summary>
    /// <remarks>
    /// 0 represents infinite number of retries.
    /// </remarks>
    public int MaxRetryAttempts { get; init; } = 10;

    /// <summary>
    /// Gets the delay for attempting to retry a failed partition in seconds.
    /// </summary>
    public int BackoffDelay { get; init; } = 1;

    /// <summary>
    /// Gets the retry delay exponential factor.
    /// </summary>
    public float ExponentialBackoffDelayFactor { get; init; } = 2;

    /// <summary>
    /// Gets the max delay time in seconds for retrying a failed partition.
    /// </summary>
    public int MaximumBackoffDelay { get; init; } = 60 * 10;
}
