// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints.V2;

/// <summary>
/// Represents either Live or one particular rebuild run, with structural equality.
/// </summary>
public sealed record UniqueConstraintWritePhase
{
    UniqueConstraintWritePhase(UniqueConstraintRunId? runId) => RunId = runId;

    /// <summary>
    /// Gets the Live phase; this does not authorize runtime admission of v2 appends.
    /// </summary>
    public static UniqueConstraintWritePhase Live { get; } = new((UniqueConstraintRunId?)null);

    /// <summary>
    /// Gets the run identifier, or null for Live.
    /// </summary>
    public UniqueConstraintRunId? RunId { get; }

    /// <summary>
    /// Creates the phase for a specific rebuild run.
    /// </summary>
    /// <param name="runId">The rebuild run.</param>
    /// <returns>The run phase.</returns>
    public static UniqueConstraintWritePhase Run(UniqueConstraintRunId runId) => new(runId);
}
