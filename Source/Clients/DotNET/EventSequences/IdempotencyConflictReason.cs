// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents why an idempotency key was refused.
/// </summary>
public enum IdempotencyConflictReason
{
    /// <summary>
    /// The key was used for an event with different content.
    /// </summary>
    Content = 0,

    /// <summary>
    /// The key was used for the same event type in a different generation.
    /// </summary>
    Generation = 1,

    /// <summary>
    /// The key exists but the stored event cannot be compared with this one.
    /// </summary>
    Unverifiable = 2
}
