// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// The knowable difference between a durable intent and its retry.
/// </summary>
internal enum FingerprintConflictReason
{
    /// <summary>
    /// Content or routing differs.
    /// </summary>
    Content = 0,

    /// <summary>
    /// The same type id was requested with another generation.
    /// </summary>
    Generation = 1
}
