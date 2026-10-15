// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Describes the durable ingress outcome.
/// </summary>
public enum OccurrenceIngressStatus
{
    /// <summary>
    /// The occurrence was recorded.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// The same identity and fingerprint were already recorded.
    /// </summary>
    AlreadyRecorded = 1,

    /// <summary>
    /// The identity was already used with a different payload.
    /// </summary>
    OccurrenceIdConflict = 2,

    /// <summary>
    /// The payload, trigger or ingress kind does not satisfy the registered contract.
    /// </summary>
    OccurrencePayloadInvalid = 3
}
