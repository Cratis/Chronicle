// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// The reason a failed-partition entry is held without recording an attempt.
/// </summary>
public enum FailedPartitionHoldReason
{
    /// <summary>
    /// No connected target declares the awaited event type.
    /// </summary>
    WaitingForHandler = 0
}
