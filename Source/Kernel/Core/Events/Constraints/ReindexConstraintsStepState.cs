// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Jobs;

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
}
