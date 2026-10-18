// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes why a durable key cannot settle a retry successfully.
/// </summary>
public enum IdempotencyConflictReason
{
    /// <summary>
    /// Content, type, source or route differs.
    /// </summary>
    Content = 0,

    /// <summary>
    /// The same event type was requested at another generation.
    /// </summary>
    Generation = 1,

    /// <summary>
    /// A protecting key is unavailable without a covering erasure.
    /// </summary>
    Unverifiable = 2
}
