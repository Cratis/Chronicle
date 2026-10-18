// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Identifies the whole recovery execution that must match before a recovery job may mutate an entry.
/// </summary>
/// <param name="EpisodeId">The hold or failure episode.</param>
/// <param name="JobId">The recovery job identifier.</param>
/// <param name="Execution">The observer-assigned execution number, advanced for each replacement job.</param>
/// <param name="State">The entry's recovery state.</param>
/// <param name="Revision">The control record revision under which the execution was started.</param>
public record FailedPartitionRecoveryOwner(
    Guid EpisodeId,
    Guid JobId,
    long Execution,
    FailedPartitionRecoveryState State,
    long Revision);
