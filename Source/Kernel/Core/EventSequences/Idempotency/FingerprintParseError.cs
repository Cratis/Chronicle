// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// A refusal to interpret persistent fingerprint text.
/// </summary>
internal enum FingerprintParseError
{
    /// <summary>
    /// The format version is not supported.
    /// </summary>
    UnknownFingerprintVersion = 0,

    /// <summary>
    /// The text does not conform to the supported format.
    /// </summary>
    MalformedFingerprint = 1
}
