// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Observation.Replaying;

/// <summary>
/// Represents a candidate for replaying.
/// </summary>
/// <param name="ObserverId">The <see cref="ObserverId"/> the candidate is for.</param>
/// <param name="ObserverKey">The <see cref="ObserverKey"/> the candidate is for. </param>
/// <param name="Reasons">Collection of <see cref="ReplayCandidateReason"/>.</param>
public record ReplayCandidate(
    ObserverId ObserverId,
    ObserverKey ObserverKey,
    IEnumerable<ReplayCandidateReason> Reasons);
