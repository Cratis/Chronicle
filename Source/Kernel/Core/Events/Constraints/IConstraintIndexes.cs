// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines a system that keeps the unique constraint indexes of an event store in step with its constraint definitions.
/// </summary>
public interface IConstraintIndexes
{
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
