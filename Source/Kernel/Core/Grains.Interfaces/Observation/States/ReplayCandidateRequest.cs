// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Grains.Observation.States;

/// <summary>
/// Represents the request for a replay candidate.
/// </summary>
[DerivedType("08ff4094-0684-420e-8f9a-60b0d838ea86")]
public class ReplayCandidateRequest : IRecommendationRequest
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
    /// Gets or sets the <see cref="ObserverType"/> for the observer.
    /// </summary>
    public ObserverType ObserverType { get; set; } = ObserverType.Unknown;

    /// <summary>
    /// Gets or sets the reasons for why this is a replay candidate.
    /// </summary>
    public IEnumerable<ReplayCandidateReason> Reasons { get; set; } = [];
}
