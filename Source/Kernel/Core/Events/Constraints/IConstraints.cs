// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Defines the constraints system for an event store.
/// </summary>
public interface IConstraints : IGrainWithStringKey
{
    /// <summary>
    /// Register a set of definitions.
    /// </summary>
    /// <param name="definitions">Collection of <see cref="IConstraintDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<IConstraintDefinition> definitions);

    /// <summary>
    /// Get the currently registered constraint definitions.
    /// </summary>
    /// <returns>A snapshot of the registered <see cref="IConstraintDefinition"/> for the event store.</returns>
    /// <remarks>
    /// Served from the grain's in-memory state so that callers (such as the event sequence) can resolve the current
    /// definitions without querying storage. The snapshot is re-read from persisted state on activation.
    /// </remarks>
    Task<IReadOnlyCollection<IConstraintDefinition>> GetDefinitions();

    /// <summary>
    /// Get the current <see cref="ConstraintsVersion"/> for the event store.
    /// </summary>
    /// <returns>A content-derived stamp that changes whenever the registered definitions change.</returns>
    /// <remarks>
    /// A cheap, cluster-safe signal callers cache and compare on each append to detect that constraints have changed
    /// since they last read them. It is derived from the content of the definitions, so it is stable across grain
    /// deactivation and identical across silos for identical definitions.
    /// </remarks>
    Task<ConstraintsVersion> GetVersion();
}
