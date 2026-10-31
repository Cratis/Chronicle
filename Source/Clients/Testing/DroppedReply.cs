// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Identifies a successful mutation whose reply can be lost after commit.
/// </summary>
public enum DroppedReply
{
    /// <summary>
    /// A receipted or keyed append (any append shape).
    /// </summary>
    Append = 0,

    /// <summary>
    /// A receipt-only write.
    /// </summary>
    RecordEffectReceipt = 1,

    /// <summary>
    /// Occurrence ingress.
    /// </summary>
    RecordOccurrence = 2,

    /// <summary>
    /// An occurrence claim.
    /// </summary>
    ClaimOccurrences = 3,

    /// <summary>
    /// An occurrence completion.
    /// </summary>
    CompleteOccurrence = 4,

    /// <summary>
    /// An occurrence failure.
    /// </summary>
    FailOccurrence = 5
}
