// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that keeps the unique constraint indexes of an event store in step with its constraint definitions.
/// </summary>
public interface IConstraintIndexes
{
    /// <summary>
    /// Refreshes constraint validation and starts an explicit durable index rebuild.
    /// </summary>
    /// <param name="eventStore">The event store holding the registered definitions.</param>
    /// <param name="namespace">The namespace whose indexes are rebuilt.</param>
    /// <param name="eventSequenceId">The event sequence to replay.</param>
    /// <param name="changes">The selected constraints requiring reindexing.</param>
    /// <returns>The identifier of the started job.</returns>
    Task<JobId> Reindex(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId, IReadOnlyCollection<ConstraintDefinitionChange> changes);

    /// <summary>
    /// Start rebuilding the indexes that a change of the registered definitions has made stale.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the definitions belong to.</param>
    /// <param name="previous">The definitions registered before the change.</param>
    /// <param name="current">The definitions registered after the change.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Every event sequence of every namespace in the event store is considered, whether or not its grain is active,
    /// and a reindex job is started for each one whose index of a unique constraint must be rebuilt - most notably a
    /// sequence a constraint now applies to but did not before.
    /// </remarks>
    Task RebuildStaleIndexes(
        EventStoreName eventStore,
        IReadOnlyCollection<IConstraintDefinition> previous,
        IReadOnlyCollection<IConstraintDefinition> current);
}
