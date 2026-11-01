// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Recommendations;
using Cratis.Serialization;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a registration waiting for a newer owner or switching without an ordering.
/// </summary>
[DerivedType("cd79d8b2-b619-435b-a9c3-648ef395f54c")]
public class ObserverRegistrationConflictRequest : IRecommendationRequest
{
    /// <summary>
    /// Gets or sets the observer being registered.
    /// </summary>
    public ObserverId ObserverId { get; set; } = ObserverId.Unspecified;

    /// <summary>
    /// Gets or sets the requested event sequence.
    /// </summary>
    public EventSequenceId IncomingEventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the active owner's event sequence.
    /// </summary>
    public EventSequenceId OwnerEventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the incoming client version.
    /// </summary>
    public string IncomingVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the active owner's version.
    /// </summary>
    public string OwnerVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conflict outcome: standby or unordered.
    /// </summary>
    public string Outcome { get; set; } = string.Empty;
}
