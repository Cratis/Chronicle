// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Identifies why a range was skipped. Unknown numeric values must be retained.
/// </summary>
public enum SkipReason
{
    /// <summary>
    /// The reason is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// An operator requested the skip.
    /// </summary>
    Operator = 1,

    /// <summary>
    /// The retry policy requested the skip.
    /// </summary>
    SkipAfterRetries = 2,

    /// <summary>
    /// Clearing failed partitions requested the skip.
    /// </summary>
    ClearFailedPartitions = 3
}
