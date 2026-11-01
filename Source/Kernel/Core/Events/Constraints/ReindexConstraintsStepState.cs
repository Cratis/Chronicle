// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents state for reindexing constraints step.
/// </summary>
public class ReindexConstraintsStepState : JobStepState
{
    /// <summary>
    /// Gets or sets the event sequence identifier to reindex.
    /// </summary>
    public EventSequenceId EventSequenceId { get; set; } = EventSequenceId.Unspecified;

    /// <summary>
    /// Gets or sets the changed constraints.
    /// </summary>
    public IList<ConstraintDefinitionChange> Changes { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of skipped duplicate claims per constraint, without claim contents or identities.
    /// </summary>
    public IDictionary<ConstraintName, long> CollisionCounts { get; set; } = new Dictionary<ConstraintName, long>();

    /// <summary>
    /// Gets or sets up to 100 skipped duplicate claim samples per constraint.
    /// </summary>
    /// <remarks>
    /// Value hashes use HMAC-SHA256 with a random in-memory key per step run. They are comparable only within
    /// one run and change after a resume. Neither claimed values nor event source identities are retained.
    /// </remarks>
    public IDictionary<ConstraintName, IList<ConstraintCollisionSample>> CollisionSamples { get; set; } = new Dictionary<ConstraintName, IList<ConstraintCollisionSample>>();

    /// <summary>
    /// Gets or sets the number of skipped duplicate claim samples beyond the per-constraint cap.
    /// </summary>
    public IDictionary<ConstraintName, long> CollisionSampleOverflowCounts { get; set; } = new Dictionary<ConstraintName, long>();
}
