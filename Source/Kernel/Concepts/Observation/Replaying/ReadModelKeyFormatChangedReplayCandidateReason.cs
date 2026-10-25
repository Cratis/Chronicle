// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when the read model storage holds composite keys in a legacy key format that only a replay can rebuild.
/// </summary>
public record ReadModelKeyFormatChangedReplayCandidateReason()
    : ReplayCandidateReason(ReplayCandidateReasonType.ReadModelKeyFormatChanged);
