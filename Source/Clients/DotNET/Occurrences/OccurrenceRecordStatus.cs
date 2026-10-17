// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Describes durable ingress or a refused identity/payload.
/// </summary>
public enum OccurrenceRecordStatus
{
    /// <summary>
    /// The occurrence is durable.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// The same identity and payload were already recorded.
    /// </summary>
    AlreadyRecorded = 1,

    /// <summary>
    /// The identity already names another payload.
    /// </summary>
    OccurrenceIdConflict = 2,

    /// <summary>
    /// The trigger or payload does not satisfy the registered contract.
    /// </summary>
    OccurrencePayloadInvalid = 3
}
