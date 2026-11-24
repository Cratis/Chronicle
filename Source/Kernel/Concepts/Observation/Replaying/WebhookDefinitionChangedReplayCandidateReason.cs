// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.Replaying;

/// <summary>
/// Represents a reason for a replay candidate when webhook definition has changed.
/// </summary>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Observation.Replaying.WebhookDefinitionChangedReplayCandidateReason")]
public record WebhookDefinitionChangedReplayCandidateReason()
    : ReplayCandidateReason(ReplayCandidateReasonType.WebhookDefinitionChanged);