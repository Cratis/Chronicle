// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Reactions.Replaying;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the request for a replay candidate.
/// </summary>
public class ReplayCandidateRequest
{
    /// <summary>
    /// Gets or sets the <see cref="ObserverId"/> for the observer.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the <see cref="ObserverKey"/> for the observer.
    /// </summary>
    public ObserverKey ObserverKey { get; set; } = ObserverKey.NotSet;

    /// <summary>
    /// Gets or sets the reasons for why this is a replay candidate.
    /// </summary>
    public IEnumerable<ReplayCandidateReason> Reasons { get; set; } = [];
}
