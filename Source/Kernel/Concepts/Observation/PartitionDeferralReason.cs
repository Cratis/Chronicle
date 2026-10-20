// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Describes why delivery is waiting for a durable deadline.
/// </summary>
public enum PartitionDeferralReason
{
    /// <summary>
    /// The target circuit breaker is open.
    /// </summary>
    CircuitOpen = 1,

    /// <summary>
    /// The target requested a delay before the next delivery.
    /// </summary>
    RetryAfter = 2,

    /// <summary>
    /// A failed delivery is waiting for its next scheduled attempt.
    /// </summary>
    RetryBackoff = 3
}
