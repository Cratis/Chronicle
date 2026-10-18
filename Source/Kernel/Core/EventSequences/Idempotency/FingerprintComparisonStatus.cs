// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// The result of settling an intent against a durable fingerprint.
/// </summary>
internal enum FingerprintComparisonStatus
{
    /// <summary>
    /// Every part matched.
    /// </summary>
    Verified = 0,

    /// <summary>
    /// Every knowable part matched; at least one protecting key was erased.
    /// </summary>
    VerifiedAfterErasure = 1,

    /// <summary>
    /// A missing or mismatched key prevents verification.
    /// </summary>
    Unverifiable = 2,

    /// <summary>
    /// The request differs in a knowable part.
    /// </summary>
    Conflict = 3
}
