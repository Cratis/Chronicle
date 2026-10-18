// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// An anticipated refusal to fingerprint an append.
/// </summary>
internal enum FingerprintFailure
{
    /// <summary>
    /// Values of one kind were encrypted under different keys or revisions.
    /// </summary>
    FingerprintKeysDiverged = 0,

    /// <summary>
    /// A boundary cannot be represented unambiguously.
    /// </summary>
    FingerprintUnsupported = 1
}
